using CommunityToolkit.Mvvm.ComponentModel;
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

        /// <summary> Game title. </summary>
        public string Title => Game.Title;

        /// <summary> Game genre. </summary>
        public string Genre => Game.Genre;

        /// <summary> Release year. </summary>
        public int ReleaseYear => Game.ReleaseYear;

        /// <summary> Gaming platform. </summary>
        public string Platform => Game.Platform;

        /// <summary> Disk size in GB. </summary>
        public double SizeGB => Game.SizeGB;

        /// <summary> Game status. </summary>
        public GameStatus Status => Game.Status;

        /// <summary> Hours played. </summary>
        public double HoursPlayed => Game.HoursPlayed;

        /// <summary> Personal rating. </summary>
        public int PersonalRating => Game.PersonalRating;

        /// <summary> Developer studio name. </summary>
        public string DeveloperName => Game.Developer?.Name ?? "Невідомо";

        public GameDetailsViewModel(Game game)
        {
            Game = game;
        }

        /// <summary>
        /// Refreshes all UI bindings after game data has been updated.
        /// </summary>
        public void RefreshGame()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Genre));
            OnPropertyChanged(nameof(ReleaseYear));
            OnPropertyChanged(nameof(Platform));
            OnPropertyChanged(nameof(SizeGB));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(HoursPlayed));
            OnPropertyChanged(nameof(PersonalRating));
            OnPropertyChanged(nameof(DeveloperName));
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
