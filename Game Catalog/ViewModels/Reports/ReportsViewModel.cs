using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Reports page: every PDF report of the app in one place.
    /// A report is generated only on request; on navigation the page just checks which reports have data.
    /// </summary>
    public partial class ReportsViewModel : ViewModelBase
    {
        /// <summary> Number of games the library report would list. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLibraryGames))]
        [NotifyCanExecuteChangedFor(nameof(SaveLibraryReportCommand))]
        private int _libraryGameCount;

        /// <summary> True when the library report has at least one game. </summary>
        public bool HasLibraryGames => LibraryGameCount > 0;

        public override void OnNavigatedTo() => Load();

        /// <summary> Checks which reports have data. </summary>
        private void Load()
        {
            try
            {
                // The library summary counts the same games as the report: standalone and not archived.
                LibraryGameCount = StatisticsRepository.GetLibrarySummary().GameCount;
            }
            catch (DatabaseException ex)
            {
                LibraryGameCount = 0;
                _ = ShowErrorAsync("Помилка завантаження звітів", ex);
            }
        }

        /// <summary> Asks where to save the library report, generates it and opens the file. </summary>
        [RelayCommand(CanExecute = nameof(HasLibraryGames))]
        private async Task SaveLibraryReportAsync()
        {
            var path = await Dialogs.Current.PickPdfSaveFileAsync(
                $"Звіт «{LibraryReport.Title}»", $"{LibraryReport.Title} {DateTime.Today:yyyy-MM-dd}");
            if (path == null) return;

            try
            {
                // The totals come from the same aggregate query as the statistics page.
                var summary = StatisticsRepository.GetLibrarySummary();
                var rows = ReportRepository.GetLibraryReport();
                await ReportLayout.SaveAsync(LibraryReport.Create(rows, summary), path);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Не вдалося сформувати звіт", ex);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Dialogs.Current.ErrorAsync("Не вдалося зберегти звіт", path, ex);
                return;
            }

            await Dialogs.Current.OpenFileAsync(path);
        }

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
