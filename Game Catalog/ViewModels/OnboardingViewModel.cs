using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
        [Range(100, 100000, ErrorMessage = "Розмір диску має бути від 100 до 100 000 ГБ")]
        [NotifyDataErrorInfo]
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

        /// <summary> Indicates whether the entered RAWG key has been successfully validated. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanFinish))]
        [NotifyPropertyChangedFor(nameof(CanSkip))]
        private bool _isRawgKeyValidated;

        /// <summary> Status message shown after API key validation. </summary>
        [ObservableProperty] private string _rawgKeyStatusMessage = string.Empty;

        /// <summary> Indicates whether the key validation is in progress. </summary>
        [ObservableProperty] private bool _isValidatingKey;

        /// <summary> Finish button is active only when key is validated. </summary>
        public bool CanFinish => IsRawgKeyValidated;

        /// <summary> Skip button is active only when key is not validated. </summary>
        public bool CanSkip => !IsRawgKeyValidated;

        [RelayCommand]
        private async Task ValidateRawgKeyAsync()
        {
            if (string.IsNullOrWhiteSpace(RawgApiKey)) return;
            IsValidatingKey = true;
            RawgKeyStatusMessage = "Перевірка...";
            var valid = await RawgService.ValidateKeyAsync(RawgApiKey);
            IsValidatingKey = false;

            if (valid)
            {
                IsRawgKeyValidated = true;
                RawgKeyStatusMessage = "✓ Ключ дійсний";
            }
            else
            {
                IsRawgKeyValidated = false;
                RawgKeyStatusMessage = "✗ Невірний ключ";
            }
        }

        partial void OnRawgApiKeyChanged(string value)
        {
            IsRawgKeyValidated = false;
            RawgKeyStatusMessage = string.Empty;
        }

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