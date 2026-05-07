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

        /// <summary>Available UI languages.</summary>
        public string[] Languages { get; } = { "UA", "EN" };

        /// <summary>Currently selected theme.</summary>
        [ObservableProperty]
        private string _selectedTheme = SettingsService.Current.Theme;

        /// <summary>Currently selected language.</summary>
        [ObservableProperty]
        private string _selectedLanguage = SettingsService.Current.Language;

        /// <summary>Disk capacity in GB for the statistics progress bar.</summary>
        [ObservableProperty]
        private double _diskCapacityGB = SettingsService.Current.DiskCapacityGB;

        partial void OnSelectedThemeChanged(string value)
        {
            Application.Current!.RequestedThemeVariant =
                value == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
            SettingsService.Current.Theme = value;
            SettingsService.Save();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            SettingsService.Current.Language = value;
            SettingsService.Save();
        }

        partial void OnDiskCapacityGBChanged(double value)
            => SettingsService.UpdateDiskCapacity(value);
    }
}