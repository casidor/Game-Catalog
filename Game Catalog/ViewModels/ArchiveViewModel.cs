using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the archived games page. </summary>
    public partial class ArchiveViewModel : FilterableGameViewModel
    {
        /// <summary> Source collection for the filter base class. </summary>
        protected override ObservableCollection<Game> SourceGames => AppData.Instance.ArchivedGames;

        /// <summary> Currently selected archived game. </summary>
        [ObservableProperty] private Game? _selectedGame;

        /// <summary> Raised when the user selects a game to view details. </summary>
        public event Action<Game>? GameSelected;

        public ArchiveViewModel() => InitializeCollection();

        /// <summary> Navigates to the game detail page. </summary>
        [RelayCommand]
        private void SelectGame(Game game) => GameSelected?.Invoke(game);
    }
}
