using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System.Linq;
using System.Threading.Tasks;

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

        var vm = new AddStudioViewModel(detailVm.Studio);
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
        var parent = TopLevel.GetTopLevel(this) as Window;
        _ = DeleteStudioAsync(detailVm, parent!);
    }

    private async Task DeleteStudioAsync(StudioDetailsViewModel detailVm, Window parent)
    {
        var gamesCount = AppData.Instance.Games
            .Count(g => g.Developer?.Id == detailVm.Studio.Id);
        var archivedCount = AppData.Instance.ArchivedGames
            .Count(g => g.Developer?.Id == detailVm.Studio.Id);
        var total = gamesCount + archivedCount;

        if (total > 0)
        {
            await ConfirmationWindow.ShowAlertAsync(parent,
                "Неможливо видалити студію",
                $"Студія «{detailVm.Name}» має {total} {(total == 1 ? "гру" : "ігор")} у бібліотеці або архіві.\n" +
                "Спочатку видаліть або перепризначте ці ігри.");
            return;
        }

        var confirmed = await ConfirmationWindow.ShowAsync(
            parent,
            title: "Видалення студії",
            message: $"Видалити студію «{detailVm.Name}»? Цю дію не можна скасувати.",
            confirmText: "Видалити",
            cancelText: "Скасувати");

        if (!confirmed) return;
        AppData.Instance.Studios.Remove(detailVm.Studio);
        detailVm.GoBackCommand.Execute(null);
    }

    private void OnGameSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StudioDetailsViewModel vm) return;
        if (sender is not ListBox lb || lb.SelectedItem is not Game game) return;
        lb.SelectedItem = null;
        vm.SelectGameCommand.Execute(game);
    }
}