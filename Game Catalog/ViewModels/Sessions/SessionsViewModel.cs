using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the sessions page: database filtering, editing and deletion of play sessions. </summary>
    public partial class SessionsViewModel : ViewModelBase
    {
        /// <summary> Filter currently applied to the list. </summary>
        private SessionFilter _applied = new();

        /// <summary> Rows of the sessions table. </summary>
        public ObservableCollection<PlaySession> Rows { get; } = new();

        /// <summary> Game dropdown of the filter panel. </summary>
        public MultiSelectField Games { get; } = new("Усі ігри");

        /// <summary> Entry methods offered in the filter panel. </summary>
        public MethodOption[] MethodOptions { get; } =
        {
            new("Усі", null),
            new("Вручну", EntryMethod.Manual),
            new("Авто", EntryMethod.Auto)
        };

        /// <summary> Draft entry method, edited in the panel. </summary>
        [ObservableProperty] private MethodOption _draftMethod;

        /// <summary> Draft first day of the start date range, edited in the panel. </summary>
        [ObservableProperty] private DateTime? _draftDateFrom;

        /// <summary> Draft last day of the start date range, edited in the panel. </summary>
        [ObservableProperty] private DateTime? _draftDateTo;

        /// <summary> Draft lower bound of the duration in hours, edited in the panel. </summary>
        [ObservableProperty] private double? _draftHoursFrom;

        /// <summary> Draft upper bound of the duration in hours, edited in the panel. </summary>
        [ObservableProperty] private double? _draftHoursTo;

        /// <summary> Indicates whether the filter panel is open. </summary>
        [ObservableProperty] private bool _isFilterPanelOpen;

        /// <summary> True when a filter is applied to the list. </summary>
        public bool HasActiveFilter => !_applied.IsEmpty;

        /// <summary> Number of filter groups that are currently applied. </summary>
        public int ActiveFilterCount =>
            (_applied.GameIds.Count > 0 ? 1 : 0) +
            (_applied.Method != null ? 1 : 0) +
            (_applied.DateFrom != null || _applied.DateTo != null ? 1 : 0) +
            (_applied.DurationFrom != null || _applied.DurationTo != null ? 1 : 0);

        /// <summary> Caption of the toolbar filter button. </summary>
        public string FiltersButtonText =>
            ActiveFilterCount > 0 ? $"⚙ Фільтри · {ActiveFilterCount}" : "⚙ Фільтри";

        /// <summary> True when there are no sessions and no filter is applied. </summary>
        public bool IsEmpty => Rows.Count == 0 && !HasActiveFilter;

        /// <summary> True when the applied filter leaves no sessions. </summary>
        public bool IsFilteredEmpty => Rows.Count == 0 && HasActiveFilter;

        public SessionsViewModel() => _draftMethod = MethodOptions[0];

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

        /// <summary> Loads sessions matching the applied filter from the database. </summary>
        private void Reload()
        {
            try
            {
                var sessions = SessionRepository.GetAll(_applied);
                Rows.Clear();
                foreach (var session in sessions)
                    Rows.Add(session);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження сесій", ex);
            }

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
            DraftMethod = MethodOptions.First(o => o.Value == _applied.Method);
            DraftDateFrom = _applied.DateFrom;
            DraftDateTo = _applied.DateTo;
            DraftHoursFrom = _applied.DurationFrom?.TotalHours;
            DraftHoursTo = _applied.DurationTo?.TotalHours;
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
            _applied = new SessionFilter
            {
                GameIds = Games.CheckedOptions.Select(o => o.Id).ToList(),
                Method = DraftMethod.Value,
                DateFrom = DraftDateFrom,
                DateTo = DraftDateTo,
                DurationFrom = DraftHoursFrom is { } fromHours ? TimeSpan.FromHours(fromHours) : null,
                DurationTo = DraftHoursTo is { } toHours ? TimeSpan.FromHours(toHours) : null
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
            DraftMethod = MethodOptions[0];
            DraftDateFrom = null;
            DraftDateTo = null;
            DraftHoursFrom = null;
            DraftHoursTo = null;
        }

        /// <summary> Removes the applied filter and resets the panel. </summary>
        [RelayCommand]
        private void ResetFilters()
        {
            _applied = new SessionFilter();
            SyncDraftFromApplied();
            IsFilterPanelOpen = false;
            Reload();
        }

        /// <summary> Opens the editor for a new session. The editor saves the session itself. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var saved = await Dialogs.Current.ShowSessionEditorAsync();
            if (saved != null)
                Load();
        }

        /// <summary> Opens the editor for the given session. The editor saves the changes itself. </summary>
        [RelayCommand]
        private async Task EditAsync(PlaySession session)
        {
            var saved = await Dialogs.Current.ShowSessionEditorAsync(session);
            if (saved != null)
                Load();
        }

        /// <summary> Deletes the session after confirmation. </summary>
        [RelayCommand]
        private async Task DeleteAsync(PlaySession session)
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення сесії",
                $"Видалити сесію гри «{session.GameTitle}» від {session.StartTime:dd.MM.yyyy HH:mm}? " +
                "Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                SessionRepository.Delete(session.PlaySessionId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення сесії", ex);
                return;
            }

            Load();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
