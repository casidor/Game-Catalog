using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit platform dialog. </summary>
    public partial class AddPlatformViewModel : ViewModelBase
    {
        /// <summary> Name of the platform entered by the user. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NameText]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _platformName = string.Empty;

        /// <summary> Optional manufacturer of the platform. </summary>
        [MaxLength(100, ErrorMessage = "Назва виробника не може перевищувати 100 символів")]
        [NameText("Назва виробника")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _manufacturer = string.Empty;

        /// <summary>
        /// Icon of the platform: a stored path, a picked file not imported yet (absolute path), or empty for no icon.
        /// A picked file is imported into the media folder only after the platform is saved.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasIcon))]
        private string _platformIconPath = string.Empty;

        /// <summary> Size info and warnings about the chosen icon. </summary>
        [ObservableProperty]
        private ImageReport _iconReport = ImageReport.Empty;

        /// <summary> True when an icon is chosen, so it can be removed. </summary>
        public bool HasIcon => PlatformIconPath.Length > 0;

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly int _platformId;

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
            PlatformIconPath = platform.PlatformIconPath;
            PlatformName = platform.PlatformName;
            Manufacturer = platform.Manufacturer;
        }

        /// <summary> Builds a platform object from the entered data. </summary>
        public Platform BuildPlatform() => new Platform
        {
            PlatformId = _platformId,
            PlatformName = PlatformName,
            Manufacturer = Manufacturer,
            PlatformIconPath = PlatformIconPath
        };

        partial void OnPlatformIconPathChanged(string value) =>
            IconReport = ImageInspector.Inspect(value, ImageKind.Icon);

        /// <summary> Lets the user pick an icon file. </summary>
        [RelayCommand]
        private async Task PickIconAsync()
        {
            // An existing platform opens its own folder, so a previously used icon can be picked again.
            var startFolder = _platformId != 0 ? MediaStore.PlatformFolder(_platformId) : null;
            var path = await Dialogs.Current.PickImageAsync("Оберіть іконку платформи", startFolder);
            if (path != null)
                SetIcon(path);
        }

        /// <summary> Downloads an icon by URL. </summary>
        [RelayCommand]
        private async Task DownloadIconAsync()
        {
            var path = await Dialogs.Current.DownloadImageAsync();
            if (path != null)
                SetIcon(path);
        }

        /// <summary> Removes the icon. </summary>
        [RelayCommand]
        private void ClearIcon() => SetIcon(string.Empty);

        /// <summary> Assigns the icon path and drops the cached bitmap of the old file. </summary>
        private void SetIcon(string path)
        {
            ImageCache.Invalidate(PlatformIconPath);
            PlatformIconPath = path;
        }

        /// <summary> Validates the form and closes the dialog. </summary>
        [RelayCommand]
        private void Confirm()
        {
            PlatformName = TextRules.NormalizeName(PlatformName);
            Manufacturer = TextRules.NormalizeName(Manufacturer);
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