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
    /// <summary> ViewModel for the game library page. </summary>
    public partial class LibraryViewModel : FilterableGameViewModel
    {
        /// <summary> Source collection for the filter base class. </summary>
        protected override ObservableCollection<Game> SourceGames => AppData.Instance.Games;

        /// <summary> Collection of available studios for game assignment. </summary>
        public ObservableCollection<Studio> Studios => AppData.Instance.Studios;

        /// <summary> Currently selected game in the list. </summary>
        [ObservableProperty] private Game? _selectedGame;

        /// <summary> Raised when the user selects a game to view details. </summary>
        public event Action<Game>? GameSelected;

        public LibraryViewModel() => InitializeCollection();

        /// <summary> Navigates to the game detail page. </summary>
        [RelayCommand]
        private void SelectGame(Game game) => GameSelected?.Invoke(game);
    }
}
