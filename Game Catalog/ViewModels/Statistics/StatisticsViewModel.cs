using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Statistics page: summary information computed by the database.
    /// Every statistic is reloaded when the page is shown, so it always reflects the current library.
    /// </summary>
    public partial class StatisticsViewModel : ViewModelBase
    {
        /// <summary> Index of the selected tab. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverviewTab), nameof(IsStudiosTab), nameof(IsGenresTab), nameof(IsActivityTab), nameof(IsForgottenTab))]
        private int _selectedTabIndex;

        /// <summary> True when the overview tab is selected. </summary>
        public bool IsOverviewTab => SelectedTabIndex == 0;

        /// <summary> True when the studios tab is selected. </summary>
        public bool IsStudiosTab => SelectedTabIndex == 1;

        /// <summary> True when the genres tab is selected. </summary>
        public bool IsGenresTab => SelectedTabIndex == 2;

        /// <summary> True when the activity tab is selected. </summary>
        public bool IsActivityTab => SelectedTabIndex == 3;

        /// <summary> True when the forgotten games tab is selected. </summary>
        public bool IsForgottenTab => SelectedTabIndex == 4;

        /// <summary> Raised with the id of a game the user wants to open. </summary>
        public event Action<int>? GameSelected;

        /// <summary> Raised with the id of a studio the user wants to open. </summary>
        public event Action<int>? StudioSelected;

        // Library summary

        /// <summary> Overall state of the library. Null until loaded or when loading failed. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameCount), nameof(TotalHours), nameof(SizeText), nameof(RatingText))]
        private LibrarySummary? _summary;

        /// <summary> Number of games in the library. </summary>
        public int GameCount => Summary?.GameCount ?? 0;

        /// <summary> Total play time in hours over finished sessions. </summary>
        public double TotalHours => Summary?.TotalHours ?? 0;

        /// <summary> Total size of installed games, or null when nothing is measured. </summary>
        public string? SizeText => Summary?.TotalSizeGb is { } s ? $"{s:0.0} ГБ" : null;

        /// <summary> Average rating with one decimal, or null when no game is rated. </summary>
        public string? RatingText => Summary?.AverageRating is { } r ? $"{r:0.0} / 10" : null;

        // Longest session

        /// <summary> The longest finished session, or null when there is none. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLongestSession), nameof(LongestStartText))]
        private LongestSession? _longestSession;

        /// <summary> True when there is at least one finished session. </summary>
        public bool HasLongestSession => LongestSession != null;

        /// <summary> Start of the longest session as a short date and time. </summary>
        public string? LongestStartText =>
            LongestSession?.StartTime.ToString("dd.MM.yy, HH:mm", CultureInfo.InvariantCulture);

        // Games by play time

        /// <summary> Games with finished sessions, from the most played to the least. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPlayTimeRows))]
        private List<GamePlayTime> _gamesByPlayTime = new();

        /// <summary> True when at least one game has play time. </summary>
        public bool HasPlayTimeRows => GamesByPlayTime.Count > 0;

        // Studios and genres

        /// <summary> Studios with games in the library, from the most played to the least. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStudioRows))]
        private List<StudioStat> _studioStats = new();

        /// <summary> True when at least one studio has games in the library. </summary>
        public bool HasStudioRows => StudioStats.Count > 0;

        /// <summary> Genres with games in the library, from the one with the most games to the least. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasGenreRows))]
        private List<GenreStat> _genreStats = new();

        /// <summary> True when at least one genre has games in the library. </summary>
        public bool HasGenreRows => GenreStats.Count > 0;

        // Activity for a period

        /// <summary> First day of the activity period, included. The last 30 days by default. </summary>
        [ObservableProperty]
        private DateTime? _activityFrom = DateTime.Today.AddDays(-29);

        /// <summary> Last day of the activity period, included. </summary>
        [ObservableProperty]
        private DateTime? _activityTo = DateTime.Today;

        /// <summary> Days of the period with finished sessions, newest first. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasActivityRows))]
        private List<DailyActivity> _activityDays = new();

        /// <summary> True when the period has at least one day with sessions. </summary>
        public bool HasActivityRows => ActivityDays.Count > 0;

        /// <summary> Text shown instead of the activity table: no sessions in the period or an incomplete or wrong period. </summary>
        [ObservableProperty]
        private string _activityEmptyText = string.Empty;

        // Forgotten games

        /// <summary> Games kept in the library for more than 14 days without any session, the longest kept first. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasForgottenRows))]
        private List<ForgottenGame> _forgottenGames = new();

        /// <summary> True when at least one game is forgotten. </summary>
        public bool HasForgottenRows => ForgottenGames.Count > 0;

        partial void OnActivityFromChanged(DateTime? value) => LoadActivity();

        partial void OnActivityToChanged(DateTime? value) => LoadActivity();

        public override void OnNavigatedTo() => Load();

        /// <summary> Reloads all statistics from the database. </summary>
        public void Load()
        {
            try
            {
                Summary = StatisticsRepository.GetLibrarySummary();
                LongestSession = StatisticsRepository.GetLongestSession();
                GamesByPlayTime = StatisticsRepository.GetGamesByPlayTime();
                StudioStats = StatisticsRepository.GetStudioStats();
                GenreStats = StatisticsRepository.GetGenreStats();
                ForgottenGames = StatisticsRepository.GetForgottenGames();
            }
            catch (DatabaseException ex)
            {
                Summary = null;
                LongestSession = null;
                GamesByPlayTime = new();
                StudioStats = new();
                GenreStats = new();
                ForgottenGames = new();
                _ = ShowErrorAsync("Помилка завантаження статистики", ex);
            }

            LoadActivity();
        }

        /// <summary>
        /// Reloads the activity table for the chosen period. An incomplete or wrong period
        /// is not sent to the database: the table gives way to a hint instead.
        /// </summary>
        private void LoadActivity()
        {
            if (ActivityFrom is not { } from || ActivityTo is not { } to)
            {
                ActivityDays = new();
                ActivityEmptyText = "Оберіть дату початку й кінця періоду";
                return;
            }

            if (from.Date > to.Date)
            {
                ActivityDays = new();
                ActivityEmptyText = "Оберіть коректний період: дата початку пізніша за дату кінця";
                return;
            }

            try
            {
                ActivityDays = StatisticsRepository.GetDailyActivity(from, to);
                ActivityEmptyText = "За цей період сесій немає";
            }
            catch (DatabaseException ex)
            {
                ActivityDays = new();
                ActivityEmptyText = "Не вдалося завантажити активність";
                _ = ShowErrorAsync("Помилка завантаження активності", ex);
            }
        }

        /// <summary> Opens the game of a row of the play time table. </summary>
        [RelayCommand]
        private void OpenPlayTimeGame(GamePlayTime? row)
        {
            if (row != null)
                GameSelected?.Invoke(row.GameId);
        }

        /// <summary> Opens the game of the longest session. </summary>
        [RelayCommand]
        private void OpenLongestSessionGame()
        {
            if (LongestSession is { } session)
                GameSelected?.Invoke(session.GameId);
        }

        /// <summary> Opens a game of the forgotten games table. </summary>
        [RelayCommand]
        private void OpenForgottenGame(ForgottenGame? row)
        {
            if (row != null)
                GameSelected?.Invoke(row.GameId);
        }

        /// <summary> Opens the studio of a row of the studios table. </summary>
        [RelayCommand]
        private void OpenStudio(StudioStat? row)
        {
            if (row != null)
                StudioSelected?.Invoke(row.StudioId);
        }

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
