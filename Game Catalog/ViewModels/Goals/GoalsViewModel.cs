using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the goals page: the goals table plus the filter panel, all applied by the database. </summary>
    public partial class GoalsViewModel : ViewModelBase
    {
        /// <summary> Filter currently applied to the list. </summary>
        private GoalFilter _applied = new();

        /// <summary> Goals table: rows, sorting, search, selection and the actions on goals. </summary>
        public GoalTableViewModel Table { get; }

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
        public bool IsEmpty => Table.Rows.Count == 0 && !HasActiveFilter;

        /// <summary> True when the applied filter leaves no goals. </summary>
        public bool IsFilteredEmpty => Table.Rows.Count == 0 && HasActiveFilter;

        public GoalsViewModel()
        {
            Table = new GoalTableViewModel((sortField, descending) =>
                GoalRepository.GetAll(_applied with { SortField = sortField, SortDescending = descending }));

            // A goal changed in the table may affect the filter (e.g. a game added from the editor).
            Table.DataChanged += Load;
        }

        /// <summary> Loads the page data when the page is shown. </summary>
        public override void OnNavigatedTo() => Load();

        /// <summary> Refreshes the game list of the filter and the table. Called when the page is shown. </summary>
        public void Load()
        {
            IsFilterPanelOpen = false;
            try
            {
                var games = GameRepository.GetAll(new GameFilter { IsArchived = false });

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

        /// <summary> Reloads the table with the applied filter and refreshes the filter and empty state indicators. </summary>
        private void Reload()
        {
            Table.Reload();

            OnPropertyChanged(nameof(HasActiveFilter));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(FiltersButtonText));
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

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}