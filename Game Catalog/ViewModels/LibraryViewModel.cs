using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the game library page.
    /// </summary>
    public partial class LibraryViewModel : ViewModelBase
    {
        /// <summary>
        /// Collection of all games in the library.
        /// </summary>
        public ObservableCollection<Game> Games => AppData.Instance.Games;

        /// <summary>
        /// Collection of available studios for game assignment.
        /// </summary>
        public ObservableCollection<Studio> Studios => AppData.Instance.Studios;

        /// <summary>
        /// Currently selected game in the list.
        /// </summary>
        [ObservableProperty]
        private Game? _selectedGame;

        /// <summary>
        /// Raised when the user selects a game to view details.
        /// </summary>
        public event Action<Game>? GameSelected;

        /// <summary>
        /// Navigates to the game detail page.
        /// </summary>
        /// <param name="game">The selected game.</param>
        [RelayCommand]
        private void SelectGame(Game game)
        {
            GameSelected?.Invoke(game);
        }
    }
}
