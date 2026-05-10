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

        /// <summary>Country name or "Невідомо" if not specified.</summary>
        public string DisplayCountry => string.IsNullOrWhiteSpace(Studio.Country) ? "Невідомо" : Studio.Country;

        /// <summary>Main genre or "Невідомо" if not specified.</summary>
        public string DisplayMainGenre => string.IsNullOrWhiteSpace(Studio.MainGenre) ? "Невідомо" : Studio.MainGenre;

        /// <summary>Foundation year or "Невідомо" if the value is zero.</summary>
        public string DisplayFoundationYear => Studio.FoundationYear == 0 ? "Невідомо" : Studio.FoundationYear.ToString();

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
    }
}
