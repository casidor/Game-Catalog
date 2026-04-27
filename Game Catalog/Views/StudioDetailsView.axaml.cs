using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class StudioDetailsView : UserControl
{
    public StudioDetailsView()
    {
        InitializeComponent();
    }
    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StudioDetailsViewModel detailVm) return;

        var vm = new AddStudioViewModel(detailVm.Studio, AppData.Instance.Studios);
        var window = new AddStudioWindow { DataContext = vm };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        await window.ShowDialog(parentWindow!);

        if (vm.Confirmed)
        {
            detailVm.Studio.Name = vm.Name;
            detailVm.Studio.Country = vm.Country;
            detailVm.Studio.FoundationYear = vm.FoundationYear;
            detailVm.Studio.MainGenre = vm.MainGenre;
            var index = AppData.Instance.Studios.IndexOf(detailVm.Studio);
            if (index >= 0)
                AppData.Instance.Studios[index] = detailVm.Studio;

            detailVm.RefreshStudio();
        }
    }
    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StudioDetailsViewModel detailVm) return;
            AppData.Instance.Studios.Remove(detailVm.Studio);

        detailVm.GoBackCommand.Execute(null);
    }
}