using CommunityToolkit.Mvvm.ComponentModel;
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
        public ObservableCollection<Game> Games { get; } = new();
        /// <summary>
        /// Currently selected game in the list.
        /// </summary>
        [ObservableProperty]
        private Game? _selectedGame;
    }
}
