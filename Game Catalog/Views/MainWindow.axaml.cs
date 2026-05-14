using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Game_Catalog.Services;
using System.Threading.Tasks;

namespace Game_Catalog.Views
{
    /// <summary>
    /// Main application window. Manages data lifecycle, keyboard shortcuts,
    /// and surfaces storage errors to the user via alert dialogs.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>Prevents stacking multiple simultaneous save-error dialogs.</summary>
        private bool _saveErrorShown;

        /// <summary>Initializes the window and loads XAML.</summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>Loads saved data and enables auto-save when the window first appears.</summary>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            DataService.SaveFailed += OnDataSaveFailed;
            SettingsService.SaveFailed += OnSettingsSaveFailed;

            if (!SettingsService.LoadSucceeded)
                _ = ConfirmationWindow.ShowAlertAsync(this,
                    "Помилка налаштувань",
                    "Файл налаштувань пошкоджений. Застосовано стандартні налаштування.");

            var loaded = DataService.LoadDefault();
            if (!loaded)
                _ = ConfirmationWindow.ShowAlertAsync(this,
                    "Помилка завантаження",
                    "Файл каталогу пошкоджений або недоступний.\nКаталог завантажено порожнім.");

            DataService.EnableAutoSave();
        }

        /// <summary>Unsubscribes from service events when the window is closed.</summary>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DataService.SaveFailed -= OnDataSaveFailed;
            SettingsService.SaveFailed -= OnSettingsSaveFailed;
        }

        /// <summary>Displays an alert when a catalog save operation fails.</summary>
        private async void OnDataSaveFailed(string message)
        {
            if (_saveErrorShown) return;
            _saveErrorShown = true;
            await ConfirmationWindow.ShowAlertAsync(this,
                "Помилка збереження",
                $"Не вдалося зберегти дані каталогу.\n{message}");
            _saveErrorShown = false;
        }

        /// <summary>Displays an alert when a settings save operation fails.</summary>
        private async void OnSettingsSaveFailed(string message)
        {
            if (_saveErrorShown) return;
            _saveErrorShown = true;
            await ConfirmationWindow.ShowAlertAsync(this,
                "Помилка збереження налаштувань",
                $"Не вдалося зберегти налаштування.\n{message}");
            _saveErrorShown = false;
        }
    }
}