using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
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

        /// <summary> First day of the activity period, included. The last 30 days by default. </summary>
        [ObservableProperty]
        private DateTime? _activityFrom = DateTime.Today.AddDays(-29);

        /// <summary> Last day of the activity period, included. </summary>
        [ObservableProperty]
        private DateTime? _activityTo = DateTime.Today;

        /// <summary>
        /// Sessions of the chosen period with their totals. They are loaded whenever the period changes,
        /// both to know whether the report has data and to build it without loading them again.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasActivityRows))]
        [NotifyCanExecuteChangedFor(nameof(SaveActivityReportCommand))]
        private List<ActivityReportRow> _activityRows = new();

        /// <summary> True when the period has at least one finished session. </summary>
        public bool HasActivityRows => ActivityRows.Count > 0;

        /// <summary> Hint shown when the activity report cannot be built: no sessions or an incomplete or wrong period. </summary>
        [ObservableProperty]
        private string _activityEmptyText = string.Empty;

        partial void OnActivityFromChanged(DateTime? value) => LoadActivity();

        partial void OnActivityToChanged(DateTime? value) => LoadActivity();

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

            LoadActivity();
        }

        /// <summary>
        /// Reloads the sessions of the chosen period. An incomplete or wrong period
        /// is not sent to the database: the report is disabled with a hint instead.
        /// </summary>
        private void LoadActivity()
        {
            if (ActivityFrom is not { } from || ActivityTo is not { } to)
            {
                ActivityRows = new();
                ActivityEmptyText = "Оберіть дату початку й кінця періоду";
                return;
            }

            if (from.Date > to.Date)
            {
                ActivityRows = new();
                ActivityEmptyText = "Дата початку пізніша за дату кінця";
                return;
            }

            try
            {
                ActivityRows = ReportRepository.GetActivityReport(from, to);
                ActivityEmptyText = "За обраний період ігрових сесій немає";
            }
            catch (DatabaseException ex)
            {
                ActivityRows = new();
                ActivityEmptyText = "Не вдалося завантажити активність";
                _ = ShowErrorAsync("Помилка завантаження активності", ex);
            }
        }

        /// <summary> Saves the library report. The totals come from the same query as the statistics page. </summary>
        [RelayCommand(CanExecute = nameof(HasLibraryGames))]
        private Task SaveLibraryReportAsync() =>
            SaveReportAsync(TodayFileName(LibraryReport.Title), LibraryReport.Title, path => ReportLayout.SaveAsync(
                LibraryReport.Create(ReportRepository.GetLibraryReport(), StatisticsRepository.GetLibrarySummary()), path));

        /// <summary> Saves the goals report. </summary>
        [RelayCommand(CanExecute = nameof(HasGoals))]
        private Task SaveGoalReportAsync() =>
            SaveReportAsync(TodayFileName(GoalReport.Title), GoalReport.Title, path => ReportLayout.SaveAsync(
                GoalReport.Create(ReportRepository.GetGoalReport(), ReportRepository.GetGoalSummary()), path));

        /// <summary> Saves the activity report from the sessions already loaded for the chosen period. </summary>
        [RelayCommand(CanExecute = nameof(HasActivityRows))]
        private Task SaveActivityReportAsync()
        {
            // The period and rows are taken now, so changing the dates while saving does not mix them up.
            var rows = ActivityRows;
            var from = ActivityFrom!.Value.Date;
            var to = ActivityTo!.Value.Date;
            return SaveReportAsync($"{ActivityReport.Title} {from:yyyy-MM-dd} – {to:yyyy-MM-dd}", ActivityReport.Title,
                path => ReportLayout.SaveAsync(ActivityReport.Create(rows, from, to), path));
        }

        /// <summary>
        /// Asks where to save a report, generates it and opens the file.
        /// The data is loaded inside <paramref name="save"/>, so database errors are reported here too.
        /// </summary>
        /// <param name="fileName"> File name offered by default, without the extension. </param>
        /// <param name="title"> Report title, shown in the picker. </param>
        /// <param name="save"> Loads the data, builds the document and writes it to the given path. </param>
        private static async Task SaveReportAsync(string fileName, string title, Func<string, Task> save)
        {
            var path = await Dialogs.Current.PickPdfSaveFileAsync($"Звіт «{title}»", fileName);
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

        /// <summary> File name of a report that shows the current state: the title and today's date. </summary>
        private static string TodayFileName(string title) => $"{title} {DateTime.Today:yyyy-MM-dd}";

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
