using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;

namespace Game_Catalog.Views;

public partial class GameDetailsView : UserControl
{
    public GameDetailsView()
    {
        InitializeComponent();
    }
    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;

        var vm = new AddGameViewModel(detailVm.Game, AppData.Instance.Studios);
        var window = new AddGameWindow { DataContext = vm };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        await window.ShowDialog(parentWindow!);

        if (vm.Confirmed)
        {
            detailVm.Game.Title = vm.Title;
            detailVm.Game.Developer = vm.SelectedStudio;
            detailVm.Game.Genre = vm.Genre;
            detailVm.Game.ReleaseYear = vm.ReleaseYear;
            detailVm.Game.Platform = vm.Platform;
            detailVm.Game.SizeGB = vm.SizeGB;
            detailVm.Game.Status = vm.Status;
            detailVm.Game.HoursPlayed = vm.HoursPlayed;
            detailVm.Game.PersonalRating = vm.PersonalRating;

            var index = AppData.Instance.Games.IndexOf(detailVm.Game);
            if (index >= 0)
                AppData.Instance.Games[index] = detailVm.Game;

            detailVm.RefreshGame();
        }
    }
    private void OnArchiveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;

        AppData.Instance.Games.Remove(detailVm.Game);
        AppData.Instance.ArchivedGames.Add(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;

        AppData.Instance.Games.Remove(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }
}