using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Goals table shared by the goals page and the goals tab of the game details: rows, sorting and search done
    /// by the database, selection, status changes, editing and deleting. The owner decides which goals are loaded.
    /// </summary>
    public partial class GoalTableViewModel : ObservableObject
    {
        /// <summary> Loads the goals of the owner in the given order. </summary>
        private readonly Func<GoalSortField, bool, List<Goal>> _loadGoals;

        /// <summary>
        /// Raised after goals were changed or deleted here. The owner reloads the table (and whatever else it shows)
        /// through Reload; the table restores the selection afterwards where it needs to.
        /// </summary>
        public event Action? DataChanged;

        /// <summary> True when the goals may only be viewed (goals of an archived game): no edits, status changes or deletes. </summary>
        public bool IsReadOnly { get; }

        /// <summary> Rows of the table. </summary>
        public ObservableCollection<GoalRow> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<GoalRow> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select a goal again after it was edited. </summary>
        [ObservableProperty] private GoalRow? _selectedRow;

        /// <summary> Text whose matches are highlighted in the table. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> "Found N of M" text shown next to the search box; empty when nothing is searched. </summary>
        [ObservableProperty] private string _searchSummary = string.Empty;

        /// <summary> Field of the current sort order; the sorting itself is done by the database. </summary>
        [ObservableProperty] private GoalSortField _sortField = GoalSortField.Priority;

        /// <summary> True for descending order. The table opens with the highest priority first. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> Number of selected goals that can be completed or cancelled (active and overdue ones). </summary>
        public int OpenSelectedCount => SelectedRows.Count(r => r.IsOpen);

        /// <summary> Number of selected goals that can be returned to active (completed and cancelled ones). </summary>
        public int ClosedSelectedCount => SelectedRows.Count(r => r.IsClosed);

        /// <summary> True when "Completed" and "Cancel" apply to at least one selected goal. </summary>
        public bool HasOpenSelected => OpenSelectedCount > 0;

        /// <summary> True when "Return to active" applies to at least one selected goal. </summary>
        public bool HasClosedSelected => ClosedSelectedCount > 0;

        /// <summary> True when the context menu has at least one status action. </summary>
        public bool HasStatusActions => HasOpenSelected || HasClosedSelected;

        /// <summary> Context menu captions with the number of goals the action applies to. </summary>
        public string CompleteSelectedHeader => $"Виконано ({OpenSelectedCount})";
        public string CancelSelectedHeader => $"Скасувати ({OpenSelectedCount})";
        public string ReopenSelectedHeader => $"Повернути в активні ({ClosedSelectedCount})";
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        /// <param name="loadGoals"> Loads the goals to show, already sorted by the given field and direction. </param>
        /// <param name="isReadOnly"> True to only view the goals. </param>
        public GoalTableViewModel(Func<GoalSortField, bool, List<Goal>> loadGoals, bool isReadOnly = false)
        {
            _loadGoals = loadGoals;
            IsReadOnly = isReadOnly;
            SelectedRows.CollectionChanged += (_, _) => OnSelectionChanged();
        }

        partial void OnSearchTextChanged(string value) => UpdateMatches();

        /// <summary> Refreshes the menu captions and the commands that depend on the selection. </summary>
        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(OpenSelectedCount));
            OnPropertyChanged(nameof(ClosedSelectedCount));
            OnPropertyChanged(nameof(HasOpenSelected));
            OnPropertyChanged(nameof(HasClosedSelected));
            OnPropertyChanged(nameof(HasStatusActions));
            OnPropertyChanged(nameof(CompleteSelectedHeader));
            OnPropertyChanged(nameof(CancelSelectedHeader));
            OnPropertyChanged(nameof(ReopenSelectedHeader));
            OnPropertyChanged(nameof(DeleteSelectedHeader));
            EditSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary> Sorts by the clicked table column: same column flips the direction. </summary>
        public void SortByColumn(GoalSortField field)
        {
            if (SortField == field)
            {
                SortDescending = !SortDescending;
            }
            else
            {
                SortField = field;
                SortDescending = false;
            }
            Reload();
        }

        /// <summary> Loads the goals of the owner from the database, already in the chosen order. </summary>
        public void Reload()
        {
            try
            {
                var goals = _loadGoals(SortField, SortDescending);
                Rows.Clear();
                foreach (var goal in goals)
                    Rows.Add(new GoalRow(goal));
                UpdateMatches();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження цілей", ex);
            }
        }

        /// <summary> Marks rows found by the database search and updates the "found" counter. </summary>
        private void UpdateMatches()
        {
            // Nothing to search for or nothing to mark: reset without asking the database.
            if (string.IsNullOrWhiteSpace(SearchText) || Rows.Count == 0)
            {
                foreach (var row in Rows)
                    row.IsMatch = false;
                SearchSummary = string.Empty;
                return;
            }

            HashSet<int> matches;
            try
            {
                matches = GoalRepository.GetMatchIds(SearchText);
            }
            catch (DatabaseException ex)
            {
                matches = new HashSet<int>();
                _ = ShowErrorAsync("Помилка пошуку цілей", ex);
            }

            foreach (var row in Rows)
                row.IsMatch = matches.Contains(row.Goal.GoalId);

            SearchSummary = $"Знайдено: {Rows.Count(r => r.IsMatch)} з {Rows.Count}";
        }

        /// <summary> Opens the editor for the goal of a double-clicked row and selects that goal again after saving. </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private async Task EditAsync(GoalRow row)
        {
            var saved = await Dialogs.Current.ShowGoalEditorAsync(row.Goal);
            if (saved == null)
                return;

            DataChanged?.Invoke();
            SelectedRow = Rows.FirstOrDefault(r => r.Goal.GoalId == row.Goal.GoalId);
        }

        /// <summary> Opens the editor for the only selected goal. </summary>
        [RelayCommand(CanExecute = nameof(CanEditSelected))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0]);

        private bool CanEdit(GoalRow row) => !IsReadOnly;

        private bool CanEditSelected() => !IsReadOnly && SelectedRows.Count == 1;

        /// <summary> Marks the goal of the row as completed now. Works for active and overdue goals. </summary>
        [RelayCommand]
        private Task CompleteAsync(Goal goal) => ChangeStatusAsync(GoalRepository.Complete, new[] { goal.GoalId });

        /// <summary> Cancels the goal of the row. Works for active and overdue goals. </summary>
        [RelayCommand]
        private Task CancelGoalAsync(Goal goal) => ChangeStatusAsync(GoalRepository.Cancel, new[] { goal.GoalId });

        /// <summary> Returns the completed or cancelled goal of the row to active. </summary>
        [RelayCommand]
        private Task ReopenAsync(Goal goal) => ChangeStatusAsync(GoalRepository.Reopen, new[] { goal.GoalId });

        /// <summary> Completes the selected active and overdue goals; the others are skipped. </summary>
        [RelayCommand]
        private Task CompleteSelectedAsync() => ChangeStatusAsync(GoalRepository.Complete, SelectedIds(r => r.IsOpen));

        /// <summary> Cancels the selected active and overdue goals; the others are skipped. </summary>
        [RelayCommand]
        private Task CancelSelectedAsync() => ChangeStatusAsync(GoalRepository.Cancel, SelectedIds(r => r.IsOpen));

        /// <summary> Returns the selected completed and cancelled goals to active; the others are skipped. </summary>
        [RelayCommand]
        private Task ReopenSelectedAsync() => ChangeStatusAsync(GoalRepository.Reopen, SelectedIds(r => r.IsClosed));

        /// <summary> Deletes the selected goals in one transaction after confirmation. </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelectedAsync()
        {
            var rows = SelectedRows.ToList();
            var confirmed = rows.Count == 1
                ? await Dialogs.Current.ConfirmAsync(
                    "Видалення цілі",
                    $"Видалити ціль «{rows[0].GoalName}»?\nЦю дію неможливо скасувати.",
                    "Видалити")
                : await Dialogs.Current.ConfirmAsync(
                    "Видалення цілей",
                    $"Видалити {GoalsCount(rows.Count)}?\nЦю дію неможливо скасувати.",
                    "Видалити");
            if (!confirmed) return;

            try
            {
                GoalRepository.Delete(rows.Select(r => r.Goal.GoalId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення цілей", ex);
                return;
            }
            DataChanged?.Invoke();
        }

        private bool HasSelection() => !IsReadOnly && SelectedRows.Count > 0;

        /// <summary> Ids of the selected goals the action applies to. </summary>
        private List<int> SelectedIds(Func<GoalRow, bool> applies) =>
            SelectedRows.Where(applies).Select(r => r.Goal.GoalId).ToList();

        /// <summary> Changes the status of the goals in one transaction and lets the owner reload. </summary>
        private async Task ChangeStatusAsync(Action<IReadOnlyCollection<int>> change, IReadOnlyCollection<int> goalIds)
        {
            if (goalIds.Count == 0) return;

            try
            {
                change(goalIds);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка зміни статусу цілі", ex);
                return;
            }
            DataChanged?.Invoke();
        }

        /// <summary> Number of goals with the matching Ukrainian word form: "1 ціль", "3 цілі", "5 цілей". </summary>
        private static string GoalsCount(int count)
        {
            var word = (count % 10, count % 100) switch
            {
                (1, not 11) => "ціль",
                (>= 2 and <= 4, not (>= 12 and <= 14)) => "цілі",
                _ => "цілей"
            };
            return $"{count} {word}";
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
