using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class LegacyStudioView : UserControl
{
    public LegacyStudioView()
    {
        InitializeComponent();
    }
    private async void OnAddStudioClick(object sender, RoutedEventArgs e)
    {
        var vm = new LegacyAddStudioViewModel();
        var window = new LegacyAddStudioWindow { DataContext = vm };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        await window.ShowDialog(parentWindow!);

        if (!vm.Confirmed) return;

        try
        {
            var studio = vm.BuildStudio();
            DatabaseService.InsertStudio(studio);
            AppData.Instance.Studios.Add(studio);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parentWindow!,
                "Помилка збереження студії", ex.Message, ex);
        }
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LegacyStudioViewModel vm && vm.SelectedStudio != null)
        {
            vm.SelectStudioCommand.Execute(vm.SelectedStudio);
            vm.SelectedStudio = null;
        }
    }
}