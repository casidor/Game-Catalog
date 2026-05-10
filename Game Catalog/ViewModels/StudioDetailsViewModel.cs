using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the studio details page.
    /// </summary>
    public partial class StudioDetailsViewModel : ViewModelBase
    {
        /// <summary>
        /// The studio being displayed.
        /// </summary> 
        public Studio Studio { get; }

        /// <summary>
        /// Raised when the user requests to go back to the previous page.
        /// </summary>
        public event Action? BackRequested;

        /// <summary> Raised when the user selects a game to view details. </summary>
        public event Action<Game>? GameSelected;

        /// <summary>
        /// Name of the studio.
        /// </summary>
        public string Name => Studio.Name;

        /// <summary>Country name or "Невідомо" if not specified.</summary>
        public string DisplayCountry => string.IsNullOrWhiteSpace(Studio.Country) ? "Невідомо" : Studio.Country;

        /// <summary>Main genre or "Невідомо" if not specified.</summary>
        public string DisplayMainGenre => string.IsNullOrWhiteSpace(Studio.MainGenre) ? "Невідомо" : Studio.MainGenre;

        /// <summary>Foundation year or "Невідомо" if the value is zero.</summary>
        public string DisplayFoundationYear => Studio.FoundationYear == 0 ? "Невідомо" : Studio.FoundationYear.ToString();

        /// <summary> Games developed by this studio. </summary>
        public IEnumerable<Game> StudioGames =>
            AppData.Instance.Games.Where(g => g.Developer?.Id == Studio.Id);

        /// <summary> Indicates whether this studio has any games in the library. </summary>
        public bool HasGames => StudioGames.Any();

        public StudioDetailsViewModel(Studio studio)
        {
            Studio = studio;
            AppData.Instance.Games.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(StudioGames));
                OnPropertyChanged(nameof(HasGames));
            };
        }

        /// <summary>
        /// Refreshes all UI bindings after game data has been updated.
        /// </summary>
        public void RefreshStudio()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(DisplayCountry));
            OnPropertyChanged(nameof(DisplayMainGenre));
            OnPropertyChanged(nameof(DisplayFoundationYear));
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            BackRequested?.Invoke();
        }

        [RelayCommand]
        private void SelectGame(Game game) => GameSelected?.Invoke(game);
    }
}
