using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class ArchiveView : UserControl
{
    public ArchiveView()
    {
        InitializeComponent();
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ArchiveViewModel vm && vm.SelectedGame != null)
        {
            vm.SelectGameCommand.Execute(vm.SelectedGame);
            vm.SelectedGame = null;
        }
    }
}