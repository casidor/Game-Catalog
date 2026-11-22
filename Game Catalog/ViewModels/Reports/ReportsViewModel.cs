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

        /// <summary> Number of goals the goals report would list. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasGoals))]
        [NotifyCanExecuteChangedFor(nameof(SaveGoalReportCommand))]
        private int _goalCount;

        /// <summary> True when the goals report has at least one goal. </summary>
        public bool HasGoals => GoalCount > 0;

        public override void OnNavigatedTo() => Load();

        /// <summary> Checks which reports have data. </summary>
        private void Load()
        {
            try
            {
                // The library summary counts the same games as the report: standalone and not archived.
                LibraryGameCount = StatisticsRepository.GetLibrarySummary().GameCount;
                GoalCount = ReportRepository.GetGoalSummary().TotalCount;
            }
            catch (DatabaseException ex)
            {
                LibraryGameCount = 0;
                GoalCount = 0;
                _ = ShowErrorAsync("Помилка завантаження звітів", ex);
            }
        }

        /// <summary> Saves the library report. The totals come from the same query as the statistics page. </summary>
        [RelayCommand(CanExecute = nameof(HasLibraryGames))]
        private Task SaveLibraryReportAsync() =>
            SaveReportAsync(LibraryReport.Title, path => ReportLayout.SaveAsync(
                LibraryReport.Create(ReportRepository.GetLibraryReport(), StatisticsRepository.GetLibrarySummary()), path));

        /// <summary> Saves the goals report. </summary>
        [RelayCommand(CanExecute = nameof(HasGoals))]
        private Task SaveGoalReportAsync() =>
            SaveReportAsync(GoalReport.Title, path => ReportLayout.SaveAsync(
                GoalReport.Create(ReportRepository.GetGoalReport(), ReportRepository.GetGoalSummary()), path));

        /// <summary>
        /// Asks where to save a report, generates it and opens the file.
        /// The data is loaded inside <paramref name="save"/>, so database errors are reported here too.
        /// </summary>
        /// <param name="title"> Report title, used for the picker and the suggested file name. </param>
        /// <param name="save"> Loads the data, builds the document and writes it to the given path. </param>
        private static async Task SaveReportAsync(string title, Func<string, Task> save)
        {
            var path = await Dialogs.Current.PickPdfSaveFileAsync($"Звіт «{title}»", $"{title} {DateTime.Today:yyyy-MM-dd}");
            if (path == null) return;

            try
            {
                await save(path);
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
