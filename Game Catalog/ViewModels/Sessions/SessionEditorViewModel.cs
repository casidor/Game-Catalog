using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit session dialog. The dialog saves the session itself, so a failed save keeps the form open. </summary>
    public partial class SessionEditorViewModel : ViewModelBase
    {
        /// <summary> Game of the session. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameButtonText))]
        private Game? _selectedGame;

        /// <summary> Day the session started. </summary>
        [ObservableProperty] private DateTime? _startDate;

        /// <summary> Time of day the session started. </summary>
        [ObservableProperty] private TimeSpan? _startTime;

        /// <summary> True while the session has not ended yet. </summary>
        [ObservableProperty] private bool _isOngoing;

        /// <summary> Day the session ended. </summary>
        [ObservableProperty] private DateTime? _endDate;

        /// <summary> Time of day the session ended. </summary>
        [ObservableProperty] private TimeSpan? _endTime;

        /// <summary> Free-form note. </summary>
        [MaxLength(500, ErrorMessage = "Нотатка не може перевищувати 500 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _note = string.Empty;

        /// <summary> Message about an incorrectly filled form, shown under the fields. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
        private string _errorMessage = string.Empty;

        /// <summary> True when there is a form message to show. </summary>
        public bool HasErrorMessage => ErrorMessage.Length > 0;

        /// <summary> All games offered in the dropdown. </summary>
        public ObservableCollection<Game> Games { get; } = new();

        /// <summary> Text typed in the game search box. </summary>
        [ObservableProperty] private string _gameSearchText = string.Empty;

        /// <summary> Games matching the search text. </summary>
        public ObservableCollection<Game> FilteredGames { get; } = new();

        /// <summary> True when the dialog is opened for a fixed game, so the game cannot be changed. </summary>
        public bool IsGameLocked { get; }

        /// <summary> True when the game can be picked from the dropdown. </summary>
        public bool IsGameEditable => !IsGameLocked;

        /// <summary> Caption of the game button. </summary>
        public string GameButtonText => SelectedGame?.GameTitle ?? "Оберіть гру";

        /// <summary> The session as saved by the dialog, or null if the dialog was cancelled. </summary>
        public PlaySession? SavedSession { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly PlaySession? _source;

        partial void OnGameSearchTextChanged(string value) => UpdateFilteredGames();

        /// <summary> When an ongoing session is marked as ended and has no end yet, the end fields get the current time. </summary>
        partial void OnIsOngoingChanged(bool value)
        {
            if (value || EndDate != null) return;

            var now = TrimToMinute(DateTime.Now);
            EndDate = now.Date;
            EndTime = now.TimeOfDay;
        }

        /// <summary> Initializes the dialog for a new session or, if a session is given, for editing it. </summary>
        /// <param name="session"> Session to edit, or null to add a new one. </param>
        /// <param name="presetGameId"> Game selected for a new session, or null. </param>
        public SessionEditorViewModel(PlaySession? session = null, int? presetGameId = null)
        {
            _source = session;
            IsGameLocked = presetGameId != null;
            WindowTitle = session == null ? "Додати сесію" : "Редагувати сесію";
            LoadGames();

            if (session == null)
            {
                SelectedGame = Games.FirstOrDefault(g => g.GameId == presetGameId);

                var now = TrimToMinute(DateTime.Now);
                var start = now.AddHours(-1);
                StartDate = start.Date;
                StartTime = start.TimeOfDay;
                EndDate = now.Date;
                EndTime = now.TimeOfDay;
                return;
            }

            SelectedGame = Games.FirstOrDefault(g => g.GameId == session.GameId);
            Note = session.Note;

            var begin = TrimToMinute(session.StartTime);
            StartDate = begin.Date;
            StartTime = begin.TimeOfDay;

            if (session.EndTime is { } finish)
            {
                var end = TrimToMinute(finish);
                EndDate = end.Date;
                EndTime = end.TimeOfDay;
            }
            else
            {
                IsOngoing = true;
            }
        }

        /// <summary> Loads all games for the dropdown. </summary>
        private void LoadGames()
        {
            try
            {
                foreach (var game in GameRepository.GetAll(new GameFilter()))
                    Games.Add(game);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження ігор", ex);
            }
            UpdateFilteredGames();
        }

        /// <summary> Rebuilds the game list for the current search text. </summary>
        private void UpdateFilteredGames()
        {
            var query = GameSearchText.Trim();

            FilteredGames.Clear();
            foreach (var game in Games)
            {
                if (query.Length == 0 || game.GameTitle.Contains(query, StringComparison.OrdinalIgnoreCase))
                    FilteredGames.Add(game);
            }
        }

        /// <summary> Adds a new game with the game editor and selects it. The editor saves the game itself. </summary>
        [RelayCommand]
        private async Task AddGameAsync()
        {
            var game = await Dialogs.Current.ShowGameEditorAsync();
            if (game == null) return;

            Games.Clear();
            LoadGames();
            SelectedGame = Games.FirstOrDefault(g => g.GameId == game.GameId);
        }

        /// <summary> Applies the game picked in the list. </summary>
        public void PickGame(Game game) => SelectedGame = game;

        /// <summary> Drops the seconds, because the time pickers work with whole minutes. </summary>
        private static DateTime TrimToMinute(DateTime value) =>
            new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);

        /// <summary> Joins the picked day and time. Untouched values keep the seconds of the original. </summary>
        private static DateTime Combine(DateTime date, TimeSpan time, DateTime? original)
        {
            var result = date.Date + time;
            return original is { } o && TrimToMinute(o) == result ? o : result;
        }

        /// <summary> Checks what the database cannot explain well. Returns a message, or null if the form is complete. </summary>
        private string? Validate(out DateTime start, out DateTime? end)
        {
            start = default;
            end = null;

            if (SelectedGame == null)
                return "Оберіть гру";
            if (StartDate is not { } startDate || StartTime is not { } startTime)
                return "Вкажіть дату й час початку";

            start = Combine(startDate, startTime, _source?.StartTime);
            if (IsOngoing)
                return null;

            if (EndDate is not { } endDate || EndTime is not { } endTime)
                return "Вкажіть дату й час завершення або позначте, що сесія триває";

            end = Combine(endDate, endTime, _source?.EndTime);
            return end <= start ? "Завершення має бути пізніше за початок" : null;
        }

        /// <summary> Validates the form and saves the session. On a database error the form stays open. </summary>
        [RelayCommand]
        private async Task ConfirmAsync()
        {
            ValidateAllProperties();
            if (HasErrors) return;

            ErrorMessage = Validate(out var start, out var end) ?? string.Empty;
            if (HasErrorMessage) return;

            var session = new PlaySession
            {
                PlaySessionId = _source?.PlaySessionId ?? 0,
                GameId = SelectedGame!.GameId,
                GameTitle = SelectedGame.GameTitle,
                StartTime = start,
                EndTime = end,
                EntryMethod = _source?.EntryMethod ?? EntryMethod.Manual,
                Note = Note
            };

            try
            {
                if (_source == null)
                    SessionRepository.Insert(session);
                else
                    SessionRepository.Update(session);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження сесії", ex);
                return;
            }

            SavedSession = session;
            CloseRequested?.Invoke();
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
            SavedSession = null;
            CloseRequested?.Invoke();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
