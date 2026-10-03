using Avalonia.Controls;
using Avalonia.Interactivity;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using System.Linq;

namespace Game_Catalog.Views;

public partial class SessionsView : UserControl
{
    public SessionsView()
    {
        InitializeComponent();
    }

    private async void OnAddSessionClick(object? sender, RoutedEventArgs e)
    {
        var parent = TopLevel.GetTopLevel(this) as Window;

        if (AppData.Instance.Games.Count == 0)
        {
            await ConfirmationWindow.ShowAlertAsync(parent!,
                "Немає ігор", "Спершу додайте хоча б одну гру в бібліотеці.");
            return;
        }

        var vm = new AddSessionViewModel(AppData.Instance.Games);
        await new AddSessionWindow { DataContext = vm }.ShowDialog(parent!);
        if (!vm.Confirmed) return;

        try
        {
            var session = vm.BuildSession();
            DatabaseService.InsertSession(session);
            AppData.Instance.Sessions.Add(session);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parent!,
                "Помилка збереження сесії", ex.Message, ex);
        }
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlaySession session }) return;
        var parent = TopLevel.GetTopLevel(this) as Window;

        var vm = new AddSessionViewModel(session, AppData.Instance.Games);
        await new AddSessionWindow { DataContext = vm }.ShowDialog(parent!);
        if (!vm.Confirmed) return;

        var candidate = vm.BuildSession();
        candidate.Id = session.Id;

        try
        {
            DatabaseService.UpdateSession(candidate);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parent!,
                "Помилка збереження сесії", ex.Message, ex);
            return;
        }

        session.Game = candidate.Game;
        session.GameId = candidate.GameId;
        session.StartTime = candidate.StartTime;
        session.EndTime = candidate.EndTime;
        session.EntryMethod = candidate.EntryMethod;
        session.Note = candidate.Note;

        var index = AppData.Instance.Sessions.IndexOf(session);
        if (index >= 0)
            AppData.Instance.Sessions[index] = session;
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlaySession session }) return;
        var parent = TopLevel.GetTopLevel(this) as Window;

        var confirmed = await ConfirmationWindow.ShowAsync(
            parent!,
            title: "Видалення сесії",
            message: "Видалити цю сесію? Цю дію не можна скасувати.",
            confirmText: "Видалити",
            cancelText: "Скасувати");
        if (!confirmed) return;

        try
        {
            DatabaseService.DeleteSession(session.Id);
        }
        catch (DatabaseException ex)
        {
            await ConfirmationWindow.ShowErrorAsync(parent!,
                "Помилка видалення", ex.Message, ex);
            return;
        }

        AppData.Instance.Sessions.Remove(session);
    }
}