using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit platform dialog. </summary>
    public partial class AddPlatformViewModel : ViewModelBase
    {
        /// <summary> Name of the platform entered by the user. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _platformName = string.Empty;

        /// <summary> Optional manufacturer of the platform. </summary>
        [MaxLength(100, ErrorMessage = "Назва виробника не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва виробника не може складатися лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _manufacturer = string.Empty;

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly int _platformId;
        private readonly string _platformIconPath = string.Empty;

        /// <summary> Initializes the dialog for adding a new platform. </summary>
        public AddPlatformViewModel()
        {
            WindowTitle = "Додати платформу";
        }

        /// <summary> Initializes the dialog for editing an existing platform. </summary>
        public AddPlatformViewModel(Platform platform)
        {
            WindowTitle = "Редагувати платформу";
            _platformId = platform.PlatformId;
            _platformIconPath = platform.PlatformIconPath;
            PlatformName = platform.PlatformName;
            Manufacturer = platform.Manufacturer;
        }

        /// <summary> Builds a platform object from the entered data. </summary>
        public Platform BuildPlatform() => new Platform
        {
            PlatformId = _platformId,
            PlatformName = PlatformName,
            Manufacturer = Manufacturer,
            PlatformIconPath = _platformIconPath
        };

        /// <summary> Validates the form and closes the dialog. </summary>
        [RelayCommand]
        private void Confirm()
        {
            ValidateAllProperties();
            if (HasErrors) return;
            Confirmed = true;
            CloseRequested?.Invoke();
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
            Confirmed = false;
            CloseRequested?.Invoke();
        }
    }
}