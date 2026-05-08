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
        /// <summary>Current onboarding step (1, 2 or 3).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStep1))]
        [NotifyPropertyChangedFor(nameof(IsStep2))]
        [NotifyPropertyChangedFor(nameof(IsStep3))]
        private int _step = 1;

        /// <summary>Selected UI theme.</summary>
        [ObservableProperty]
        private string _selectedTheme = "Dark";

        /// <summary>Disk capacity in GB for the statistics progress bar.</summary>
        [ObservableProperty]
        private double _diskCapacityGB = 500;

        /// <summary>RAWG API key entered by the user during onboarding.</summary> 
        [ObservableProperty]                                                         
        private string _rawgApiKey = string.Empty;                                  

        /// <summary>Whether the first step is active.</summary>
        public bool IsStep1 => Step == 1;

        /// <summary>Whether the second step is active.</summary>
        public bool IsStep2 => Step == 2;

        /// <summary>Whether the third step is active.</summary> 
        public bool IsStep3 => Step == 3;                        

        /// <summary>Raised when onboarding is complete.</summary>
        public event Action? Completed;

        /// <summary>Advances to the next step.</summary>
        [RelayCommand]
        private void Next() => Step++;

        /// <summary> Moves to the previous step in the sequence. </summary>
        [RelayCommand]
        private void Back() => Step--;

        /// <summary>Selects the given theme and applies it immediately.</summary>
        [RelayCommand]
        private void SelectTheme(string theme)
        {
            SelectedTheme = theme;
            Application.Current!.RequestedThemeVariant =
                theme == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        }

        /// <summary>Skips RAWG setup and finishes onboarding without an API key.</summary> 
        [RelayCommand]                                                                        
        private void Skip()                                                                   
        {                                                                                     
            RawgApiKey = string.Empty;                                                        
            Finish();                                                                         
        }                                                                                    

        /// <summary>Saves all settings and signals onboarding completion.</summary>
        [RelayCommand]
        private void Finish()
        {
            SettingsService.Current.Theme = SelectedTheme;
            SettingsService.Current.DiskCapacityGB = DiskCapacityGB;
            SettingsService.Current.RawgApiKey = RawgApiKey;
            SettingsService.Current.IsFirstRun = false;
            SettingsService.Save();
            Completed?.Invoke();
        }
    }
}