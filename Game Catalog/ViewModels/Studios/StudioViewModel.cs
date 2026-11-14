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
    /// <summary> ViewModel for the studios page: database filtering, in-memory search highlight and deletion. </summary>
    public partial class StudioViewModel : ViewModelBase
    {
        /// <summary> Filter currently applied to the list. </summary>
        private StudioFilter _applied = new();

        /// <summary> Rows of the studios table. </summary>
        public ObservableCollection<StudioRow> Rows { get; } = new();

        /// <summary> Country checkboxes of the filter panel. </summary>
        public ObservableCollection<FilterOption> CountryOptions { get; } = new();

        /// <summary> Genre checkboxes of the filter panel. </summary>
        public ObservableCollection<FilterOption> GenreOptions { get; } = new();

        /// <summary> Text whose matches are highlighted in the table. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

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

        partial void OnSearchTextChanged(string value) => UpdateMatches();

        /// <summary> Loads the page data when the page is shown. </summary>
        public override void OnNavigatedTo() => Load();

        /// <summary> Refreshes filter lists and the table. Called when the page is shown. </summary>
        public void Load()
        {
            IsFilterPanelOpen = false;
            try
            {
                var countries = StudioRepository.GetCountries();
                var genres = GenreRepository.GetAll();

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

        /// <summary> Loads studios matching the applied filter from the database. </summary>
        private void Reload()
        {
            try
            {
                var studios = StudioRepository.GetAll(_applied);
                Rows.Clear();
                foreach (var studio in studios)
                    Rows.Add(new StudioRow(studio));
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

        /// <summary> Marks rows whose name or country contains the search text. </summary>
        private void UpdateMatches()
        {
            var query = SearchText.Trim();
            foreach (var row in Rows)
            {
                row.IsMatch = query.Length > 0 &&
                    (row.StudioName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     row.Studio.Country.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
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

        /// <summary> Opens the details page of the studio. </summary>
        [RelayCommand]
        private void OpenStudio(Studio studio) => StudioSelected?.Invoke(studio);

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

        /// <summary> Opens the editor for the given studio and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync(Studio studio)
        {
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
        }

        /// <summary> Deletes the studio after confirmation unless it still has games. </summary>
        [RelayCommand]
        private async Task DeleteAsync(Studio studio)
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення студії",
                $"Видалити студію «{studio.StudioName}»? Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            bool deleted;
            try
            {
                deleted = StudioRepository.Delete(studio.StudioId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студії", ex);
                return;
            }

            if (!deleted)
            {
                await Dialogs.Current.AlertAsync(
                    "Неможливо видалити студію",
                    $"Студія «{studio.StudioName}» має ігри в бібліотеці або архіві.\n" +
                    "Спочатку видаліть або перепризначте ці ігри.");
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