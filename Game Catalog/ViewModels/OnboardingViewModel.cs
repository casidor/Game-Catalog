using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Services;
using System;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the onboarding window shown on first launch.
    /// Guides the user through initial setup in two steps.
    /// </summary>
    public partial class OnboardingViewModel : ViewModelBase
    {
        /// <summary>Current onboarding step (1 or 2).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStep1))]
        [NotifyPropertyChangedFor(nameof(IsStep2))]
        private int _step = 1;

        /// <summary>Selected UI theme.</summary>
        [ObservableProperty]
        private string _selectedTheme = "Dark";

        /// <summary>Disk capacity in GB for the statistics progress bar.</summary>
        [ObservableProperty]
        private double _diskCapacityGB = 500;

        /// <summary>Whether the first step is active.</summary>
        public bool IsStep1 => Step == 1;

        /// <summary>Whether the second step is active.</summary>
        public bool IsStep2 => Step == 2;

        /// <summary>Raised when onboarding is complete.</summary>
        public event Action? Completed;

        /// <summary>Advances to the second step.</summary>
        [RelayCommand]
        private void Next() => Step = 2;

        /// <summary>Selects the given theme and applies it immediately.</summary>
        [RelayCommand]
        private void SelectTheme(string theme)
        {
            SelectedTheme = theme;
            Application.Current!.RequestedThemeVariant =
                theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        }

        /// <summary>Saves settings and signals completion.</summary>
        [RelayCommand]
        private void Finish()
        {
            SettingsService.Current.Theme = SelectedTheme;
            SettingsService.Current.DiskCapacityGB = DiskCapacityGB;
            SettingsService.Current.IsFirstRun = false;
            SettingsService.Save();
            Completed?.Invoke();
        }
    }
}