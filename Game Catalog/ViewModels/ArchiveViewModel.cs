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
    /// ViewModel for the archived games page.
    /// </summary>
    public partial class ArchiveViewModel : ViewModelBase
    {
        /// <summary>
        /// Collection of archived games.
        /// </summary>
        public ObservableCollection<Game> ArchivedGames => AppData.Instance.ArchivedGames;

        /// <summary>
        /// Currently selected archived game.
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
