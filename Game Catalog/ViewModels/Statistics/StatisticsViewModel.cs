using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
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
        [NotifyPropertyChangedFor(nameof(IsOverviewTab))]
        private int _selectedTabIndex;

        /// <summary> True when the overview tab is selected. </summary>
        public bool IsOverviewTab => SelectedTabIndex == 0;

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

        public override void OnNavigatedTo() => Load();

        /// <summary> Reloads all statistics from the database. </summary>
        public void Load()
        {
            try
            {
                Summary = StatisticsRepository.GetLibrarySummary();
            }
            catch (DatabaseException ex)
            {
                Summary = null;
                _ = ShowErrorAsync("Помилка завантаження статистики", ex);
            }
        }

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
