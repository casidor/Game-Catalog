using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the add studio dialog window, allowing users to input details about a new game development studio.
    /// </summary>
    public partial class AddStudioViewModel : ViewModelBase
    {
        /// <summary>
        /// Name of the studio entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Country where the studio is based, entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _country = string.Empty;

        /// <summary>
        /// Year the studio was founded, entered by the user.
        /// </summary>
        [ObservableProperty]
        private int _foundationYear;

        /// <summary>
        /// Main genre the studio is known for, entered by the user.
        /// </summary>
        [ObservableProperty]
        private string _mainGenre = string.Empty;

        /// <summary>
        /// Indicates whether the user confirmed the dialog.
        /// </summary>
        public bool Confirmed { get; private set; }

        /// <summary>
        /// Event triggered when the dialog should be closed.
        /// </summary>
        public event Action? CloseRequested;

        /// <summary>
        /// Creates a new instance of the Studio class using the current property values.
        /// </summary>
        /// <returns>A Studio object initialized with the Name, Country, FoundationYear, and MainGenre properties of the current
        /// instance.</returns>
        public Studio BuildStudio()
        {
            return new Studio
            {
                Name = this.Name,
                Country = this.Country,
                FoundationYear = this.FoundationYear,
                MainGenre = this.MainGenre
            };
        }

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
