using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Services;

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
        [ObservableProperty]
        private double _diskCapacityGB = SettingsService.Current.DiskCapacityGB;

        /// <summary>RAWG API key used for game metadata fetch. Empty string disables RAWG features.</summary>
        [ObservableProperty]
        private string _rawgApiKey = SettingsService.Current.RawgApiKey;

        /// <summary>Returns true if a non-empty RAWG API key is configured.</summary>
        public bool HasRawgKey => !string.IsNullOrWhiteSpace(RawgApiKey);

        partial void OnRawgApiKeyChanged(string value)
        {
            SettingsService.Current.RawgApiKey = value;
            SettingsService.Save();
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
            => SettingsService.UpdateDiskCapacity(value);
    }
}