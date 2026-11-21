using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Services;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the settings page with the general and storage tabs.
    /// General settings are applied immediately and persisted via SettingsService.
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        /// <summary> Index of the selected tab (0 = general, 1 = storage). </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGeneralTab), nameof(IsStorageTab))]
        private int _selectedTabIndex;

        public bool IsGeneralTab => SelectedTabIndex == 0;
        public bool IsStorageTab => SelectedTabIndex == 1;

        /// <summary> Storage tab. </summary>
        public StorageSettingsViewModel Storage { get; } = new();

        /// <summary>Available UI themes.</summary>
        public string[] Themes { get; } = { "Dark", "Light" };

        /// <summary>Currently selected theme.</summary>
        [ObservableProperty]
        private string _selectedTheme = SettingsService.Current.Theme;

        partial void OnSelectedThemeChanged(string value)
        {
            Application.Current!.RequestedThemeVariant =
                value == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
            SettingsService.Current.Theme = value;
            SettingsService.Save();
        }
    }
}
