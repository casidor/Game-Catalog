using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.ObjectModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the archived games page. </summary>
    public partial class ArchiveViewModel : FilterableGameViewModel
    {
        /// <summary> Source collection for the filter base class. </summary>
        protected override ObservableCollection<Game> SourceGames => AppData.Instance.ArchivedGames;

        /// <summary> Currently selected archived game. </summary>
        [ObservableProperty] private Game? _selectedGame;

        /// <summary> Indicates whether the archive is empty. </summary>
        public bool IsEmpty => AppData.Instance.ArchivedGames.Count == 0;

        /// <summary> Raised when the user selects a game to view details. </summary>
        public event Action<Game>? GameSelected;

        /// <summary> Initializes the ViewModel and subscribes to collection changes. </summary>
        public ArchiveViewModel()
        {
            InitializeCollection();
            AppData.Instance.ArchivedGames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        }

        /// <summary> Navigates to the game detail page. </summary>
        [RelayCommand]
        private void SelectGame(Game game) => GameSelected?.Invoke(game);
    }
}