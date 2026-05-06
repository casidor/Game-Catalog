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
    /// <summary>
    /// ViewModel for the game library page.
    /// </summary>
    public partial class LibraryViewModel : ViewModelBase
    {
        /// <summary> Collection of all games in the library. </summary>
        public ObservableCollection<Game> Games => AppData.Instance.Games;

        /// <summary> Collection of available studios for game assignment. </summary>
        public ObservableCollection<Studio> Studios => AppData.Instance.Studios;

        /// <summary> Currently selected game in the list. </summary>
        [ObservableProperty]
        private Game? _selectedGame;

        /// <summary> Raised when the user selects a game to view details. </summary>
        public event Action<Game>? GameSelected;

        /// <summary> Text used to search games by title. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> Selected genre filter. Null means no filter applied. </summary>
        [ObservableProperty] private string? _selectedGenre;

        /// <summary> Selected platform filter. Null means no filter applied. </summary>
        [ObservableProperty] private string? _selectedPlatform;

        /// <summary> Selected status filter. Null means no filter applied. </summary>
        [ObservableProperty] private GameStatus? _selectedStatusFilter;

        /// <summary>
        /// Navigates to the game detail page.
        /// </summary>
        /// <param name="game">The selected game.</param>
        [RelayCommand]
        private void SelectGame(Game game)
        {
            GameSelected?.Invoke(game);
        }
        public LibraryViewModel()
        {
            Games.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(AvailableGenres));
                OnPropertyChanged(nameof(AvailablePlatforms));
                OnPropertyChanged(nameof(FilteredGames));
            };
        }

        /// <summary> Filtered and searched game list based on active criteria. </summary>
        public IEnumerable<Game> FilteredGames => Games
            .Where(g => string.IsNullOrWhiteSpace(SearchText) ||
                        g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .Where(g => SelectedGenre == null || g.Genre == SelectedGenre)
            .Where(g => SelectedPlatform == null || g.Platform == SelectedPlatform)
            .Where(g => SelectedStatusFilter == null || g.Status == SelectedStatusFilter);

        /// <summary> Distinct genres derived from the current game library. </summary>
        public IEnumerable<string?> AvailableGenres =>
            new string?[] { null }.Concat(Games.Select(g => g.Genre).Distinct().OrderBy(g => g));

        /// <summary> Distinct platforms derived from the current game library. </summary>
        public IEnumerable<string?> AvailablePlatforms =>
            new string?[] { null }.Concat(Games.Select(g => g.Platform).Distinct().OrderBy(p => p));

        /// <summary> All game statuses including a null entry to clear the filter. </summary>
        public IEnumerable<GameStatus?> AvailableStatuses =>
            new GameStatus?[] { null }.Concat(Enum.GetValues<GameStatus>().Cast<GameStatus?>());

        partial void OnSearchTextChanged(string value) => RefreshFilters();
        partial void OnSelectedGenreChanged(string? value) => RefreshFilters();
        partial void OnSelectedPlatformChanged(string? value) => RefreshFilters();
        partial void OnSelectedStatusFilterChanged(GameStatus? value) => RefreshFilters();

        private void RefreshFilters() => OnPropertyChanged(nameof(FilteredGames));
    }
}
