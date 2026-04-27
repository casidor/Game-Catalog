using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class StudioView : UserControl
{
    public StudioView()
    {
        InitializeComponent();
    }
    private async void OnAddStudioClick(object sender, RoutedEventArgs e)
    {
        var vm = new AddStudioViewModel();
        var window = new AddStudioWindow { DataContext = vm };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        await window.ShowDialog(parentWindow!);

        if (vm.Confirmed)
            AppData.Instance.Studios.Add(vm.BuildStudio());
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is StudioViewModel vm && vm.SelectedStudio != null)
        {
            vm.SelectStudioCommand.Execute(vm.SelectedStudio);
            vm.SelectedStudio = null;
        }
    }
}