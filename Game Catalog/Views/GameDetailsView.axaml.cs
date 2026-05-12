using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.ViewModels;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

public partial class GameDetailsView : UserControl
{
    public GameDetailsView()
    {
        InitializeComponent();
    }

    /// <summary>Opens the edit dialog and applies changes to the game on confirmation.</summary>
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
            detailVm.Game.Description = vm.Description;
            detailVm.Game.CoverImagePath = vm.CoverImagePath;

            var index = AppData.Instance.Games.IndexOf(detailVm.Game);
            if (index >= 0)
                AppData.Instance.Games[index] = detailVm.Game;

            detailVm.RefreshGame();
        }
    }

    /// <summary>Moves the game to the archive and removes it from the main list.</summary>
    private void OnArchiveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;
        AppData.Instance.Games.Remove(detailVm.Game);
        AppData.Instance.ArchivedGames.Add(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }

    /// <summary>Shows a confirmation dialog and permanently deletes the game if confirmed.</summary>
    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;
        var parent = TopLevel.GetTopLevel(this) as Window;
        _ = DeleteGameAsync(detailVm, parent!);
    }

    /// <summary>Shows a confirmation dialog and permanently deletes the game if confirmed.</summary>
    private async Task DeleteGameAsync(GameDetailsViewModel detailVm, Window parent)
    {
        var confirmed = await ConfirmationWindow.ShowAsync(
            parent,
            title: "Видалення гри",
            message: $"Видалити «{detailVm.Title}»? Цю дію не можна скасувати.",
            confirmText: "Видалити",
            cancelText: "Скасувати");

        if (!confirmed) return;
        if (detailVm.IsArchived)
            AppData.Instance.ArchivedGames.Remove(detailVm.Game);
        else
            AppData.Instance.Games.Remove(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }

    /// <summary>Restores the game from the archive and adds it back to the main list.</summary>
    private void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;

        AppData.Instance.ArchivedGames.Remove(detailVm.Game);
        AppData.Instance.Games.Add(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }
}