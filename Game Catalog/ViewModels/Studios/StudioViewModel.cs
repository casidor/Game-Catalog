using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the studios page: filtering, sorting and search done by the database,
    /// selection of several rows, editing and deleting.
    /// </summary>
    public partial class StudioViewModel : ViewModelBase
    {
        /// <summary> Filter currently applied to the list. </summary>
        private StudioFilter _applied = new();

        /// <summary> Rows of the studios table. </summary>
        public ObservableCollection<StudioRow> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<StudioRow> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select a studio again after it was edited. </summary>
        [ObservableProperty] private StudioRow? _selectedRow;

        /// <summary> Country checkboxes of the filter panel. </summary>
        public ObservableCollection<FilterOption> CountryOptions { get; } = new();

        /// <summary> Genre checkboxes of the filter panel. </summary>
        public ObservableCollection<FilterOption> GenreOptions { get; } = new();

        /// <summary> Text whose matches are highlighted in the table. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> "Found N of M" text shown next to the search box; empty when nothing is searched. </summary>
        [ObservableProperty] private string _searchSummary = string.Empty;

        /// <summary> Field of the current sort order; the sorting itself is done by the database. </summary>
        [ObservableProperty] private StudioSortField _sortField = StudioSortField.Name;

        /// <summary> True for descending order. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> Draft lower bound of the foundation year, edited in the panel. </summary>
        [ObservableProperty] private int? _draftYearFrom;

        /// <summary> Draft upper bound of the foundation year, edited in the panel. </summary>
        [ObservableProperty] private int? _draftYearTo;

        /// <summary> Indicates whether the filter panel is open. </summary>
        [ObservableProperty] private bool _isFilterPanelOpen;

        /// <summary> True when a filter is applied to the list. </summary>
        public bool HasActiveFilter => !_applied.IsEmpty;

        /// <summary> Number of filter groups that are currently applied. </summary>
        public int ActiveFilterCount =>
            (_applied.Countries.Count > 0 ? 1 : 0) +
            (_applied.GenreIds.Count > 0 ? 1 : 0) +
            (_applied.YearFrom != null || _applied.YearTo != null ? 1 : 0);

        /// <summary> Caption of the toolbar filter button. </summary>
        public string FiltersButtonText =>
            ActiveFilterCount > 0 ? $"⚙ Фільтри · {ActiveFilterCount}" : "⚙ Фільтри";

        /// <summary> True when there are no studios and no filter is applied. </summary>
        public bool IsEmpty => Rows.Count == 0 && !HasActiveFilter;

        /// <summary> True when the applied filter leaves no studios. </summary>
        public bool IsFilteredEmpty => Rows.Count == 0 && HasActiveFilter;

        /// <summary> Context menu caption with the number of selected studios. </summary>
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        public StudioViewModel()
        {
            SelectedRows.CollectionChanged += (_, _) => OnSelectionChanged();
        }

        partial void OnSearchTextChanged(string value) => UpdateMatches();

        /// <summary> Refreshes the menu caption and the commands that depend on the selection. </summary>
        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(DeleteSelectedHeader));
            EditSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary> Sorts by the clicked table column: same column flips the direction. </summary>
        public void SortByColumn(StudioSortField field)
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

        /// <summary> Refreshes filter lists and the table. Called when the page is shown. </summary>
        public void Load()
        {
            IsFilterPanelOpen = false;
            try
            {
                var countries = StudioRepository.GetCountries();
                var genres = GenreRepository.GetUsedByStudios();

                _applied = _applied with
                {
                    Countries = _applied.Countries.Where(c => countries.Contains(c)).ToList(),
                    GenreIds = _applied.GenreIds.Where(id => genres.Any(g => g.GenreId == id)).ToList()
                };

                CountryOptions.Clear();
                foreach (var country in countries)
                    CountryOptions.Add(new FilterOption(country));

                GenreOptions.Clear();
                foreach (var genre in genres)
                    GenreOptions.Add(new FilterOption(genre.GenreName, genre.GenreId));

                SyncDraftFromApplied();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження фільтрів", ex);
            }

            Reload();
        }

        /// <summary> Loads studios matching the applied filter from the database, already in the chosen order. </summary>
        private void Reload()
        {
            try
            {
                var studios = StudioRepository.GetAll(_applied with
                {
                    SortField = SortField,
                    SortDescending = SortDescending
                });
                Rows.SyncWith(studios.Select(s => new StudioRow(s)).ToList(), r => r.Studio.StudioId, SelectedRows);
                UpdateMatches();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження студій", ex);
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
                matches = StudioRepository.GetMatchIds(TextRules.NormalizeName(SearchText));
            }
            catch (DatabaseException ex)
            {
                matches = new HashSet<int>();
                _ = ShowErrorAsync("Помилка пошуку студій", ex);
            }

            foreach (var row in Rows)
                row.IsMatch = matches.Contains(row.Studio.StudioId);

            SearchSummary = $"Знайдено: {Rows.Count(r => r.IsMatch)} з {Rows.Count}";
        }

        /// <summary> Copies the applied filter into the panel controls. </summary>
        private void SyncDraftFromApplied()
        {
            foreach (var option in CountryOptions)
                option.IsChecked = _applied.Countries.Contains(option.Name);
            foreach (var option in GenreOptions)
                option.IsChecked = _applied.GenreIds.Contains(option.Id);
            DraftYearFrom = _applied.YearFrom;
            DraftYearTo = _applied.YearTo;
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
            _applied = new StudioFilter
            {
                Countries = CountryOptions.Where(o => o.IsChecked).Select(o => o.Name).ToList(),
                GenreIds = GenreOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                YearFrom = DraftYearFrom,
                YearTo = DraftYearTo
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
            foreach (var option in CountryOptions)
                option.IsChecked = false;
            foreach (var option in GenreOptions)
                option.IsChecked = false;
            DraftYearFrom = null;
            DraftYearTo = null;
        }

        /// <summary> Removes the applied filter and resets the panel. </summary>
        [RelayCommand]
        private void ResetFilters()
        {
            _applied = new StudioFilter();
            SyncDraftFromApplied();
            IsFilterPanelOpen = false;
            Reload();
        }

        /// <summary> Raised when the user opens a studio to view its details. </summary>
        public event Action<Studio>? StudioSelected;

        /// <summary> Opens the details page of the studio of a double-clicked row. </summary>
        [RelayCommand]
        private void OpenStudio(StudioRow row) => StudioSelected?.Invoke(row.Studio);

        /// <summary> Opens the editor and saves a new studio. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var studio = await Dialogs.Current.ShowStudioEditorAsync();
            if (studio == null) return;

            try
            {
                StudioRepository.Insert(studio);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження студії", ex);
                return;
            }
            Load();
        }

        /// <summary> Opens the editor for the only selected studio and selects it again after saving. </summary>
        [RelayCommand(CanExecute = nameof(HasSingleSelection))]
        private async Task EditSelectedAsync()
        {
            var studio = SelectedRows[0].Studio;
            var edited = await Dialogs.Current.ShowStudioEditorAsync(studio);
            if (edited == null) return;

            try
            {
                StudioRepository.Update(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження студії", ex);
                return;
            }
            Load();
            SelectedRow = Rows.FirstOrDefault(r => r.Studio.StudioId == studio.StudioId);
        }

        private bool HasSingleSelection() => SelectedRows.Count == 1;

        /// <summary>
        /// Deletes the selected studios after one confirmation. Studios that still have games cannot be deleted,
        /// so they are named in the confirmation and skipped; the rest are deleted together.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelectedAsync()
        {
            var rows = SelectedRows.ToList();

            HashSet<int> withGames;
            try
            {
                withGames = StudioRepository.GetIdsWithGames(rows.Select(r => r.Studio.StudioId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студій", ex);
                return;
            }

            var blocked = rows.Where(r => withGames.Contains(r.Studio.StudioId)).ToList();
            var free = rows.Where(r => !withGames.Contains(r.Studio.StudioId)).ToList();

            // Nothing can be deleted: only explain why.
            if (free.Count == 0)
            {
                await Dialogs.Current.AlertAsync(
                    rows.Count == 1 ? "Неможливо видалити студію" : "Неможливо видалити студії",
                    rows.Count == 1
                        ? $"Студія «{rows[0].StudioName}» має ігри в бібліотеці або архіві.\n" +
                          "Спочатку видаліть ці ігри або перепризначте їх іншій студії."
                        : "Усі вибрані студії мають ігри в бібліотеці або архіві:\n" +
                          $"{NamesList(blocked)}\n\n" +
                          "Спочатку видаліть ці ігри або перепризначте їх іншим студіям.");
                return;
            }

            var question = free.Count == 1
                ? $"Видалити студію «{free[0].StudioName}»?"
                : blocked.Count == 0
                    ? $"Видалити {Plural.Count(free.Count, "студію", "студії", "студій")}?"
                    : $"Видалити решту {Plural.Count(free.Count, "студію", "студії", "студій")}?";
            var skipped = blocked.Count == 0
                ? string.Empty
                : $"Ці студії мають ігри в бібліотеці або архіві, тому їх буде пропущено:\n{NamesList(blocked)}\n\n";

            var confirmed = await Dialogs.Current.ConfirmAsync(
                free.Count == 1 ? "Видалення студії" : "Видалення студій",
                $"{skipped}{question}\nЦю дію неможливо скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                StudioRepository.Delete(free.Select(r => r.Studio.StudioId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студій", ex);
                return;
            }
            Load();

            // The skipped studios stay selected, so it is clear which ones are left.
            var skippedIds = blocked.Select(r => r.Studio.StudioId).ToHashSet();
            foreach (var row in Rows.Where(r => skippedIds.Contains(r.Studio.StudioId)))
                SelectedRows.Add(row);
        }

        private bool HasSelection() => SelectedRows.Count > 0;

        /// <summary> Names of the studios as a bulleted list; a long list is cut after the first few names. </summary>
        private static string NamesList(IReadOnlyList<StudioRow> rows)
        {
            const int shown = 10;
            var lines = rows.Take(shown).Select(r => $"• {r.StudioName}");
            var text = string.Join("\n", lines);
            return rows.Count > shown ? $"{text}\n…і ще {rows.Count - shown}" : text;
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}