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
    /// <summary> ViewModel for the goals page: filtering, sorting and search done by the database. </summary>
    public partial class GoalsViewModel : ViewModelBase
    {
        /// <summary> Filter currently applied to the list. </summary>
        private GoalFilter _applied = new();

        /// <summary> Rows of the goals table. </summary>
        public ObservableCollection<GoalRow> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<GoalRow> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select a goal again after it was edited. </summary>
        [ObservableProperty] private GoalRow? _selectedRow;

        /// <summary> Game dropdown of the filter panel. </summary>
        public MultiSelectField Games { get; } = new("Усі ігри");

        /// <summary> Priority checkboxes of the filter panel; the id is the priority itself. </summary>
        public FilterOption[] PriorityOptions { get; } =
        {
            new("1 — найвищий", 1),
            new("2 — високий", 2),
            new("3 — середній", 3),
            new("4 — низький", 4),
            new("5 — найнижчий", 5)
        };

        /// <summary> Status checkboxes of the filter panel; the id is the GoalStatus value. </summary>
        public FilterOption[] StatusOptions { get; } =
        {
            new("Активна", (int)GoalStatus.Active),
            new("Виконана", (int)GoalStatus.Completed),
            new("Скасована", (int)GoalStatus.Cancelled),
            new("Невиконана", (int)GoalStatus.Overdue)
        };

        /// <summary> Text whose matches are highlighted in the table. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> "Found N of M" text shown next to the search box; empty when nothing is searched. </summary>
        [ObservableProperty] private string _searchSummary = string.Empty;

        /// <summary> Field of the current sort order; the sorting itself is done by the database. </summary>
        [ObservableProperty] private GoalSortField _sortField = GoalSortField.Priority;

        /// <summary> True for descending order. The page opens with the highest priority first. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> Draft first day of the due date range, edited in the panel. </summary>
        [ObservableProperty] private DateTime? _draftDueFrom;

        /// <summary> Draft last day of the due date range, edited in the panel. </summary>
        [ObservableProperty] private DateTime? _draftDueTo;

        /// <summary> Indicates whether the filter panel is open. </summary>
        [ObservableProperty] private bool _isFilterPanelOpen;

        /// <summary> True when a filter is applied to the list. </summary>
        public bool HasActiveFilter => !_applied.IsEmpty;

        /// <summary> Number of filter groups that are currently applied. </summary>
        public int ActiveFilterCount =>
            (_applied.GameIds.Count > 0 ? 1 : 0) +
            (_applied.Priorities.Count > 0 ? 1 : 0) +
            (_applied.Statuses.Count > 0 ? 1 : 0) +
            (_applied.DueFrom != null || _applied.DueTo != null ? 1 : 0);

        /// <summary> Caption of the toolbar filter button. </summary>
        public string FiltersButtonText =>
            ActiveFilterCount > 0 ? $"⚙ Фільтри · {ActiveFilterCount}" : "⚙ Фільтри";

        /// <summary> True when there are no goals and no filter is applied. </summary>
        public bool IsEmpty => Rows.Count == 0 && !HasActiveFilter;

        /// <summary> True when the applied filter leaves no goals. </summary>
        public bool IsFilteredEmpty => Rows.Count == 0 && HasActiveFilter;

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

        public GoalsViewModel()
        {
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

        /// <summary> Loads the page data when the page is shown. </summary>
        public override void OnNavigatedTo() => Load();

        /// <summary> Refreshes the game list of the filter and the table. Called when the page is shown. </summary>
        public void Load()
        {
            IsFilterPanelOpen = false;
            try
            {
                var games = GameRepository.GetAll(new GameFilter());

                _applied = _applied with
                {
                    GameIds = _applied.GameIds.Where(id => games.Any(g => g.GameId == id)).ToList()
                };

                Games.Refill(games.Select(g => new FilterOption(g.GameTitle, g.GameId)));
                SyncDraftFromApplied();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження фільтрів", ex);
            }

            Reload();
        }

        /// <summary> Loads goals matching the applied filter from the database, already in the chosen order. </summary>
        private void Reload()
        {
            try
            {
                var goals = GoalRepository.GetAll(_applied with
                {
                    SortField = SortField,
                    SortDescending = SortDescending
                });
                Rows.Clear();
                foreach (var goal in goals)
                    Rows.Add(new GoalRow(goal));
                UpdateMatches();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження цілей", ex);
            }

            OnPropertyChanged(nameof(HasActiveFilter));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(FiltersButtonText));
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

        /// <summary> Copies the applied filter into the panel controls. </summary>
        private void SyncDraftFromApplied()
        {
            Games.SetChecked(_applied.GameIds);
            foreach (var option in PriorityOptions)
                option.IsChecked = _applied.Priorities.Contains(option.Id);
            foreach (var option in StatusOptions)
                option.IsChecked = _applied.Statuses.Contains((GoalStatus)option.Id);
            DraftDueFrom = _applied.DueFrom;
            DraftDueTo = _applied.DueTo;
        }

        /// <summary> Opens the filter panel showing the currently applied values. </summary>
        [RelayCommand]
        private void OpenFilters()
        {
            SyncDraftFromApplied();
            IsFilterPanelOpen = true;
        }

        /// <summary> Applies the panel values, closes the panel and reloads the list. </summary>
        [RelayCommand]
        private void ApplyFilters()
        {
            _applied = new GoalFilter
            {
                GameIds = Games.CheckedOptions.Select(o => o.Id).ToList(),
                Priorities = PriorityOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                Statuses = StatusOptions.Where(o => o.IsChecked).Select(o => (GoalStatus)o.Id).ToList(),
                DueFrom = DraftDueFrom,
                DueTo = DraftDueTo
            };
            IsFilterPanelOpen = false;
            Reload();
        }

        /// <summary> Closes the panel and discards unapplied changes. </summary>
        [RelayCommand]
        private void CancelFilters() => IsFilterPanelOpen = false;

        /// <summary> Clears the panel controls without touching the applied filter or the table. </summary>
        [RelayCommand]
        private void ClearDraft()
        {
            Games.SetChecked(Array.Empty<int>());
            foreach (var option in PriorityOptions)
                option.IsChecked = false;
            foreach (var option in StatusOptions)
                option.IsChecked = false;
            DraftDueFrom = null;
            DraftDueTo = null;
        }

        /// <summary> Removes the applied filter and resets the panel. </summary>
        [RelayCommand]
        private void ResetFilters()
        {
            _applied = new GoalFilter();
            SyncDraftFromApplied();
            IsFilterPanelOpen = false;
            Reload();
        }

        /// <summary> Opens the editor for a new goal. The editor saves the goal itself. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var saved = await Dialogs.Current.ShowGoalEditorAsync();
            if (saved != null)
                Load();
        }

        /// <summary> Opens the editor for the goal of a double-clicked row and selects that goal again after saving. </summary>
        [RelayCommand]
        private async Task EditAsync(GoalRow row)
        {
            var saved = await Dialogs.Current.ShowGoalEditorAsync(row.Goal);
            if (saved == null)
                return;

            Load();
            SelectedRow = Rows.FirstOrDefault(r => r.Goal.GoalId == row.Goal.GoalId);
        }

        /// <summary> Opens the editor for the only selected goal. </summary>
        [RelayCommand(CanExecute = nameof(CanEditSelected))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0]);

        private bool CanEditSelected() => SelectedRows.Count == 1;

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
            Load();
        }

        private bool HasSelection() => SelectedRows.Count > 0;

        /// <summary> Ids of the selected goals the action applies to. </summary>
        private List<int> SelectedIds(Func<GoalRow, bool> applies) =>
            SelectedRows.Where(applies).Select(r => r.Goal.GoalId).ToList();

        /// <summary> Changes the status of the goals in one transaction and reloads the page. </summary>
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
            Load();
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