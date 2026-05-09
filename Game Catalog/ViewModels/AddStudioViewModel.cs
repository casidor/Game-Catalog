using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
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
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто короткая")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Country where the studio is based, entered by the user.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Назва країни не може перевищувати 50 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _country = string.Empty;

        /// <summary>
        /// Year the studio was founded, entered by the user.
        /// </summary>
        [CurrentYearRange(1950, ErrorMessage = "Некоректний рік заснування")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int _foundationYear = DateTime.Now.Year;

        /// <summary>
        /// Main genre the studio is known for, entered by the user.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Жанр не може перевищувати 50 символів")]
        [NotifyDataErrorInfo]
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
        /// Title displayed in the window header.
        /// </summary>
        public string WindowTitle { get; }

        public AddStudioViewModel()
        {
            WindowTitle = "Додати студію";
        }
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
        /// Initializes the view model with an existing game for editing.
        /// </summary>
        /// <param name="game">The game to edit.</param>
        /// <param name="studios">Available studios collection.</param>
        public AddStudioViewModel(Studio studio, ObservableCollection<Studio> studios)
        {
            WindowTitle = "Редагувати студію";
            Name = studio.Name;
            Country = studio.Country;
            FoundationYear = studio.FoundationYear;
            MainGenre = studio.MainGenre;
        }

        /// <summary>
        /// Confirms the dialog and signals the window to close.
        /// </summary>
        [RelayCommand]
        private void Confirm()
        {
            ValidateAllProperties();
            if (HasErrors) return;
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
