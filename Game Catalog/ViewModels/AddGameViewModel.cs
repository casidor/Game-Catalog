using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the add game dialog window.
    /// </summary>
    public partial class AddGameViewModel : ViewModelBase
    {
        /// <summary>
        /// Game title entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Selected developer studio.
        /// </summary>
        [ObservableProperty]
        private Studio? _selectedStudio;

        /// <summary>
        /// Game genre entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _genre = string.Empty;

        /// <summary>
        /// Release year entered by the user.
        /// </summary>
        [ObservableProperty]
        private int _releaseYear = 2024;

        /// <summary>
        /// Platform entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _platform = string.Empty;

        /// <summary>
        /// Disk size in GB entered by the user.
        /// </summary>
        [ObservableProperty]
        private double _sizeGB;

        /// <summary>
        /// Current game status selected by the user.
        /// </summary>
        [ObservableProperty]
        private GameStatus _status = GameStatus.NotInstalled;

        /// <summary>
        /// Hours played entered by the user.
        /// </summary>
        [ObservableProperty]
        private double _hoursPlayed;

        /// <summary>
        /// Personal rating entered by the user (1 to 10).
        /// </summary>
        [ObservableProperty]
        private int _personalRating = 5;

        /// <summary>
        /// List of available studios for selection.
        /// </summary>
        public ObservableCollection<Studio> Studios { get; }

        /// <summary>
        /// List of available game statuses.
        /// </summary>
        public Array Statuses { get; } = Enum.GetValues(typeof(GameStatus));

        /// <summary>
        /// Indicates whether the user confirmed the dialog.
        /// </summary>
        public bool Confirmed { get; private set; }
        /// <summary>
        /// Event triggered when the dialog should be closed.
        /// </summary>
        public event Action? CloseRequested;
        public AddGameViewModel(ObservableCollection<Studio> studios)
        {
            Studios = studios;
        }

        /// <summary>
        /// Builds and returns the game object from entered data.
        /// </summary>
        public Game BuildGame() => new Game
        {
            Title = Title,
            Developer = SelectedStudio,
            Genre = Genre,
            ReleaseYear = ReleaseYear,
            Platform = Platform,
            SizeGB = SizeGB,
            Status = Status,
            HoursPlayed = HoursPlayed,
            PersonalRating = PersonalRating
        };

        /// <summary>
        /// Confirms the dialog and signals the window to close.
        /// </summary>
        [RelayCommand]
        private void Confirm()
        {
            Confirmed = true;
            CloseRequested?.Invoke();
        }

        /// <summary>
        /// Cancels the dialog.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            Confirmed = false;
            CloseRequested?.Invoke();
        }
    }
}
