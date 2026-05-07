using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using Game_Catalog.Views;
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

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}