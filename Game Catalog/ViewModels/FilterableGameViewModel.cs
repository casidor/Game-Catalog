using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary> Abstract base ViewModel providing search and filter logic for game collections. </summary>
    public abstract partial class FilterableGameViewModel : ViewModelBase
    {
        /// <summary> Source game collection provided by the derived class. </summary>
        protected abstract ObservableCollection<Game> SourceGames { get; }

        /// <summary> Text used to search games by title. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> Selected genre filter. Null means no filter applied. </summary>
        [ObservableProperty] private string? _selectedGenre;

        /// <summary> Selected platform filter. Null means no filter applied. </summary>
        [ObservableProperty] private string? _selectedPlatform;

        /// <summary> Selected status filter. Null means no filter applied. </summary>
        [ObservableProperty] private GameStatus? _selectedStatusFilter;

        /// <summary> Filtered and searched game list based on active criteria. </summary>
        public IEnumerable<Game> FilteredGames => SourceGames
            .Where(g => string.IsNullOrWhiteSpace(SearchText) ||
                        g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .Where(g => SelectedGenre == null || g.Genre == SelectedGenre)
            .Where(g => SelectedPlatform == null || g.Platform == SelectedPlatform)
            .Where(g => SelectedStatusFilter == null || g.Status == SelectedStatusFilter);

        /// <summary> Distinct genres derived from the source collection. </summary>
        public IEnumerable<string?> AvailableGenres =>
            new string?[] { null }.Concat(SourceGames.Select(g => g.Genre).Distinct().OrderBy(g => g));

        /// <summary> Distinct platforms derived from the source collection. </summary>
        public IEnumerable<string?> AvailablePlatforms =>
            new string?[] { null }.Concat(SourceGames.Select(g => g.Platform).Distinct().OrderBy(p => p));

        /// <summary> All game statuses including a null entry to clear the filter. </summary>
        public IEnumerable<GameStatus?> AvailableStatuses =>
            new GameStatus?[] { null }.Concat(Enum.GetValues<GameStatus>().Cast<GameStatus?>());

        /// <summary> Subscribes to source collection changes to keep filters and options up to date. </summary>
        protected void InitializeCollection()
        {
            SourceGames.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(AvailableGenres));
                OnPropertyChanged(nameof(AvailablePlatforms));
                OnPropertyChanged(nameof(FilteredGames));
            };
        }

        partial void OnSearchTextChanged(string value) => RefreshFilters();
        partial void OnSelectedGenreChanged(string? value) => RefreshFilters();
        partial void OnSelectedPlatformChanged(string? value) => RefreshFilters();
        partial void OnSelectedStatusFilterChanged(GameStatus? value) => RefreshFilters();

        private void RefreshFilters() => OnPropertyChanged(nameof(FilteredGames));
    }
}
