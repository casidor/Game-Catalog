using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Game_Catalog.Services;
using System.IO;
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
            if (!SettingsService.LoadSucceeded && File.Exists(DataService.DefaultPath))
                _ = ConfirmationWindow.ShowAlertAsync(this,
                    "Помилка налаштувань",
                    "Файл налаштувань пошкоджений або відсутній. Застосовано стандартні налаштування.");
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
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                e.Handled = true;
                _ = ConfirmationWindow.ShowAlertAsync(this,
                "Довідка",
                "РОЗДІЛИ\n" +
                "🎮 Бібліотека — ваша колекція ігор. Перегляд у вигляді сітки або списку, " +
                "пошук, фільтрація за жанром, платформою, розробником і статусом.\n\n" +
                "🏢 Студії — база розробників. Перед додаванням гри потрібно додати студію.\n\n" +
                "📦 Архів — ігри, які ви перенесли з бібліотеки. Можна відновити або видалити.\n\n" +
                "📊 Статистика — кількість ігор, зіграні години, зайнятий простір на диску " +
                "та середня оцінка колекції.\n\n" +
                "⚙ Налаштування — тема інтерфейсу, розмір диску та ключ RAWG API " +
                "для автоматичного підбору інформації про гру.\n\n" +
                "КЛАВІШІ\n" +
                "F1 — ця довідка\n" +
                "Enter — підтвердити дію\n" +
                "Esc — скасувати / закрити діалог\n" +
                "Tab / Shift+Tab — перехід між полями форми");

            }
            base.OnKeyDown(e);
        }
    }
}