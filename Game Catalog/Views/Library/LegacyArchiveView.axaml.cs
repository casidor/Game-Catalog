using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class LegacyArchiveView : UserControl
{
    public LegacyArchiveView()
    {
        InitializeComponent();
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LegacyArchiveViewModel vm && vm.SelectedGame != null)
        {
            vm.SelectGameCommand.Execute(vm.SelectedGame);
            vm.SelectedGame = null;
        }
    }
}