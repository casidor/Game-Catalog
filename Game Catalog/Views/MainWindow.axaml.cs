using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Game_Catalog.Services;
using System.Threading.Tasks;

namespace Game_Catalog.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary> Loads saved data when the window is first shown. </summary>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            DataService.LoadDefault();
        }

        /// <summary> Handles Ctrl+S and Ctrl+O keyboard shortcuts. </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                if (e.Key == Key.S) { DataService.Save(); e.Handled = true; }
                else if (e.Key == Key.O) { _ = OpenFileAsync(); e.Handled = true; }
            }
            base.OnKeyDown(e);
        }

        /// <summary> Opens a file picker dialog and loads the selected JSON file. </summary>
        private async Task OpenFileAsync()
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open catalog",
                AllowMultiple = false,
                FileTypeFilter = [new("Game Catalog JSON") { Patterns = ["*.json"] }]
            });

            if (files.Count > 0)
                DataService.Load(files[0].Path.LocalPath);
        }
    }
}