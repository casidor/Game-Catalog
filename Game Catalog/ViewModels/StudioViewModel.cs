using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the studio management page. </summary>
    public partial class StudioViewModel : ViewModelBase
    {
        /// <summary> Collection of all studios in the library. </summary>
        public ObservableCollection<Studio> Studios => AppData.Instance.Studios;

        /// <summary> Currently selected studio in the list. </summary>
        [ObservableProperty]
        private Studio? _selectedStudio;

        /// <summary> Text used to search studios by name. </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary> Selected country filter. Null means no filter applied. </summary>
        [ObservableProperty]
        private string? _selectedCountry;

        /// <summary> Selected main genre filter. Null means no filter applied. </summary>
        [ObservableProperty]
        private string? _selectedGenre;

        /// <summary> Raised when the user selects a studio to view details. </summary>
        public event Action<Studio>? StudioSelected;

        public StudioViewModel()
        {
            // Subscribe to collection changes to keep filters and options up to date
            Studios.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(AvailableCountries));
                OnPropertyChanged(nameof(AvailableGenres));
                OnPropertyChanged(nameof(FilteredStudios));
            };
        }

        /// <summary> Filtered and searched studio list based on active criteria. </summary>
        public IEnumerable<Studio> FilteredStudios => Studios
            .Where(s => string.IsNullOrWhiteSpace(SearchText) ||
                        s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .Where(s => SelectedCountry == null || s.Country == SelectedCountry)
            .Where(s => SelectedGenre == null || s.MainGenre == SelectedGenre);

        /// <summary> Distinct countries derived from the source collection. </summary>
        public IEnumerable<string?> AvailableCountries =>
            new string?[] { null }.Concat(Studios.Select(s => s.Country).Distinct().OrderBy(c => c));

        /// <summary> Distinct main genres derived from the source collection. </summary>
        public IEnumerable<string?> AvailableGenres =>
            new string?[] { null }.Concat(Studios.Select(s => s.MainGenre).Distinct().OrderBy(g => g));

        partial void OnSearchTextChanged(string value) => RefreshFilters();
        partial void OnSelectedCountryChanged(string? value) => RefreshFilters();
        partial void OnSelectedGenreChanged(string? value) => RefreshFilters();

        /// <summary> Refreshes the filtered list property. </summary>
        private void RefreshFilters() => OnPropertyChanged(nameof(FilteredStudios));

        /// <summary> Navigates to the studio detail page. </summary>
        /// <param name="studio">The selected studio.</param>
        [RelayCommand]
        private void SelectStudio(Studio studio)
        {
            StudioSelected?.Invoke(studio);
        }
    }
}
