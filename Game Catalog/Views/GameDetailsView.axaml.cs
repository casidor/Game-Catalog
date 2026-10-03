using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Game_Catalog.Models;
using Game_Catalog.Services;
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
        if (!vm.Confirmed) return;

        var game = detailVm.Game;
        var candidate = game.Clone();
        candidate.Title = vm.Title;
        candidate.Developer = vm.SelectedStudio;
        candidate.Genre = vm.Genre;
        candidate.ReleaseYear = vm.ReleaseYear;
        candidate.Platform = vm.Platform;
        candidate.SizeGB = vm.SizeGB;
        candidate.Status = vm.Status;
        candidate.PersonalRating = vm.PersonalRating;
        candidate.Description = vm.Description;
        candidate.CoverImagePath = vm.CoverImagePath;

        try
        {
            DatabaseService.UpdateGame(candidate);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parentWindow!,
                "Помилка збереження", ex.Message, ex);
            return;
        }

        game.ApplyFrom(candidate);

        var index = AppData.Instance.Games.IndexOf(game);
        if (index >= 0)
            AppData.Instance.Games[index] = game;

        detailVm.RefreshGame();
    }

    /// <summary>Moves the game to the archive and removes it from the main list.</summary>
    private async void OnArchiveClick(object sender, RoutedEventArgs e) => await SetArchivedAsync(true);


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

        try
        {
            DatabaseService.DeleteGame(detailVm.Game.Id);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parent,
                "Помилка видалення", ex.Message, ex);
            return;
        }

        AppData.Instance.RemoveGameCascade(detailVm.Game);
        detailVm.GoBackCommand.Execute(null);
    }

    /// <summary> Restores the game from the archive and adds it back to the main list. </summary>
    private async void OnRestoreClick(object sender, RoutedEventArgs e) => await SetArchivedAsync(false);

    /// <summary> Sets the archived status of the game and updates the database and main list accordingly. </summary>
    private async Task SetArchivedAsync(bool archived)
    {
        if (DataContext is not GameDetailsViewModel detailVm) return;
        var parent = TopLevel.GetTopLevel(this) as Window;
        var game = detailVm.Game;

        try
        {
            DatabaseService.SetArchived(game.Id, archived);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parent!,
                "Помилка збереження", ex.Message, ex);
            return;
        }

        game.IsArchived = archived;

        var index = AppData.Instance.Games.IndexOf(game);
        if (index >= 0)
            AppData.Instance.Games[index] = game;

        detailVm.GoBackCommand.Execute(null);
    }
}