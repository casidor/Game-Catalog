using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Game_Catalog.Data;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using System;
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

        /// <summary>Set once the exit has been confirmed, so the repeated close goes through.</summary>
        private bool _exitConfirmed;

        /// <summary>Currently open developer query window, if any.</summary>
        private QueryEditorWindow? _queryWindow;

        /// <summary>Initializes the window and loads XAML.</summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>Loads saved data and enables auto-save when the window first appears.</summary>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            SettingsService.SaveFailed += OnSettingsSaveFailed;
            if (!SettingsService.LoadSucceeded && File.Exists(Db.DefaultPath))
                _ = ConfirmationWindow.ShowAlertAsync(this,
                    "Помилка налаштувань",
                    "Файл налаштувань пошкоджений або відсутній. Застосовано стандартні налаштування.");

            if (DataContext is MainWindowViewModel vm)
            {
                vm.CloseRequested += Close;
                vm.PropertyChanged += (_, args) =>
                {
                    // The old page leaves the visual tree together with its focused control,
                    // so keys would no longer reach the window: give the focus back to the root.
                    if (args.PropertyName == nameof(MainWindowViewModel.CurrentPage))
                        Dispatcher.UIThread.Post(() => RootGrid.Focus());
                };
                vm.Start();
            }
        }

        /// <summary>
        /// Asks about sessions still in progress before closing. The question is asynchronous,
        /// so the first close is cancelled and repeated after the answer. OS shutdown is not held.
        /// </summary>
        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (_exitConfirmed || e.Cancel || e.CloseReason == WindowCloseReason.OSShutdown
                || DataContext is not MainWindowViewModel vm)
                return;

            e.Cancel = true;
            if (await vm.ConfirmExitAsync())
            {
                _exitConfirmed = true;
                Close();
            }
        }

        /// <summary>Unsubscribes from service events when the window is closed.</summary>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            SettingsService.SaveFailed -= OnSettingsSaveFailed;
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
                "⏱ Сесії — журнал ігрових сесій з фільтрами за грою, датою початку, тривалістю та способом додавання.\n\n" +
                "📦 Архів — ігри, які ви перенесли з бібліотеки. Можна відновити або видалити.\n\n" +
                "⚙ Налаштування — тема інтерфейсу.\n\n" +
                "КЛАВІШІ\n" +
                "F1 — ця довідка\n" +
                "Enter — підтвердити дію\n" +
                "Esc — скасувати / закрити діалог\n" +
                "Tab / Shift+Tab — перехід між полями форми");

            }
            if (e.Key == Key.F12)
            {
                e.Handled = true;
                if (_queryWindow != null)
                {
                    _queryWindow.Close();
                }
                else
                {
                    _queryWindow = new QueryEditorWindow { DataContext = new QueryEditorViewModel() };
                    _queryWindow.Closed += (_, _) => _queryWindow = null;
                    _queryWindow.Show();
                }
            }
            base.OnKeyDown(e);
        }
    }
}