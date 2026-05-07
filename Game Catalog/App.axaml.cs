using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using Game_Catalog.Views;
using System.IO;
using System.Linq;

namespace Game_Catalog
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                SettingsService.Load();
                Application.Current!.RequestedThemeVariant =
                    SettingsService.Current.Theme == "Light"
                        ? ThemeVariant.Light
                        : ThemeVariant.Dark;

                if (SettingsService.Current.IsFirstRun && !File.Exists(DataService.DefaultPath))
                {
                    var onboarding = new OnboardingWindow();
                    desktop.MainWindow = onboarding;
                    onboarding.Closed += (_, _) =>
                    {
                        var mainWindow = new MainWindow
                        {
                            DataContext = new MainWindowViewModel()
                        };
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                    };
                }
                else
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel()
                    };
                }
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}