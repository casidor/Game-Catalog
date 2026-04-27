using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }
    private async void OnAddGameClick(object sender, RoutedEventArgs e)
    {
        var vm = new AddGameViewModel(AppData.Instance.Studios);
        var window = new AddGameWindow { DataContext = vm };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        await window.ShowDialog(parentWindow!);

        if (vm.Confirmed)
            AppData.Instance.Games.Add(vm.BuildGame());
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LibraryViewModel vm && vm.SelectedGame != null)
        {
            vm.SelectGameCommand.Execute(vm.SelectedGame);
            vm.SelectedGame = null;
        }
    }
}