using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the settings page.
    /// Applies changes immediately and persists them via SettingsService.
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        /// <summary>Available UI themes.</summary>
        public string[] Themes { get; } = { "Dark", "Light" };

        /// <summary>Currently selected theme.</summary>
        [ObservableProperty]
        private string _selectedTheme = SettingsService.Current.Theme;

        /// <summary>Disk capacity in GB for the statistics progress bar.</summary>
        [Range(100, 100000, ErrorMessage = "Розмір диску має бути від 100 до 100 000 ГБ")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private double _diskCapacityGB = SettingsService.Current.DiskCapacityGB;

        /// <summary>RAWG API key used for game metadata fetch. Empty string disables RAWG features.</summary>
        [ObservableProperty]
        private string _rawgApiKey = SettingsService.Current.RawgApiKey;

        /// <summary>Returns true if a non-empty RAWG API key is configured.</summary>
        public bool HasRawgKey => !string.IsNullOrWhiteSpace(RawgApiKey);

        /// <summary>Status message shown after API key validation.</summary>
        [ObservableProperty]
        private string _rawgKeyStatusMessage = string.Empty;

        /// <summary>Indicates whether the key validation is in progress.</summary>
        [ObservableProperty]
        private bool _isValidatingKey;

        /// <summary>Indicates whether the RAWG server is currently reachable.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionStatus))]
        [NotifyPropertyChangedFor(nameof(IsOnline))]
        private bool _isKeyValidated = false;

        /// <summary>Current RAWG connection status label.</summary>
        public string ConnectionStatus => IsKeyValidated ? "RAWG підключено" : "Офлайн режим";

        /// <summary>True if RAWG server is reachable with the saved API key.</summary>
        public bool IsOnline => IsKeyValidated;

        /// <summary>
        /// Initializes the ViewModel and checks RAWG availability in the background
        /// if an API key is already configured.
        /// </summary>
        public SettingsViewModel()
        {
            if (!string.IsNullOrWhiteSpace(RawgApiKey))
                _ = CheckConnectionAsync();
        }

        /// <summary>Checks RAWG server availability at startup using the saved API key.</summary>
        private async Task CheckConnectionAsync()
        {
            IsKeyValidated = await RawgService.ValidateKeyAsync(RawgApiKey);
        }

        /// <summary>Validates the entered API key against the RAWG server and saves it if valid.</summary>
        [RelayCommand]
        private async Task ValidateRawgKeyAsync()
        {
            if (string.IsNullOrWhiteSpace(RawgApiKey))
            {
                RawgKeyStatusMessage = "Введіть API ключ";
                return;
            }
            IsValidatingKey = true;
            RawgKeyStatusMessage = "Перевірка...";
            var valid = await RawgService.ValidateKeyAsync(RawgApiKey);
            IsValidatingKey = false;

            if (valid)
            {
                IsKeyValidated = true;
                RawgKeyStatusMessage = "✓ Ключ дійсний";
                SettingsService.Current.RawgApiKey = RawgApiKey;
                SettingsService.Save();
                OnPropertyChanged(nameof(HasRawgKey));

                await Task.Delay(3000);
                RawgKeyStatusMessage = string.Empty;
            }
            else
            {
                IsKeyValidated = false;
                RawgKeyStatusMessage = "✗ Невірний ключ — не збережено";
            }
        }

        partial void OnRawgApiKeyChanged(string value)
        {
            IsKeyValidated = false;
            RawgKeyStatusMessage = string.Empty;
            OnPropertyChanged(nameof(HasRawgKey));
        }

        partial void OnSelectedThemeChanged(string value)
        {
            Application.Current!.RequestedThemeVariant =
                value == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
            SettingsService.Current.Theme = value;
            SettingsService.Save();
        }

        partial void OnDiskCapacityGBChanged(double value)
        {
            ValidateProperty(value, nameof(DiskCapacityGB));
            if (HasErrors)
                return;
            SettingsService.UpdateDiskCapacity(value);
        }
    }
}