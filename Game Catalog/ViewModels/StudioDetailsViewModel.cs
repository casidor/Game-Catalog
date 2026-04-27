using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Name of the studio.
        /// </summary>
        public string Name => Studio.Name;

        /// <summary>
        /// Country where the studio is based.
        /// </summary>
        public string Country => Studio.Country;

        /// <summary>
        /// The year the studio was founded.
        /// </summary>
        public int FoundationYear => Studio.FoundationYear;

        /// <summary>
        /// Genre the studio is known for.
        /// </summary>
        public string MainGenre => Studio.MainGenre;
        public StudioDetailsViewModel(Studio studio)
        {
            Studio = studio;
        }

        /// <summary>
        /// Refreshes all UI bindings after game data has been updated.
        /// </summary>
        public void RefreshStudio()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Country));
            OnPropertyChanged(nameof(FoundationYear));
            OnPropertyChanged(nameof(MainGenre));
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
