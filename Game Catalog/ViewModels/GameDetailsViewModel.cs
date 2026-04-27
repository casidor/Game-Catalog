using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the game detail page.
    /// </summary>
    public partial class GameDetailsViewModel : ViewModelBase
    {
        /// <summary>
        /// The game being displayed.
        /// </summary>
        public Game Game { get; }

        /// <summary>
        /// Raised when the user requests to go back to the previous page.
        /// </summary>
        public event Action? BackRequested;

        public GameDetailsViewModel(Game game)
        {
            Game = game;
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            BackRequested?.Invoke();
        }
    }
}
