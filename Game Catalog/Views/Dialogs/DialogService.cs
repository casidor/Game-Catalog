using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

/// <summary> Avalonia implementation of <see cref="IDialogService"/> based on the main window. </summary>
public sealed class DialogService : IDialogService
{
    private static Window? Owner =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    /// <inheritdoc/>
    public async Task<bool> ConfirmAsync(string title, string message,
        string confirmText, string cancelText = "Скасувати")
    {
        if (Owner is not { } owner) return false;

        var vm = new ConfirmationViewModel(title, message, confirmText, cancelText);
        await new ConfirmationWindow { DataContext = vm }.ShowDialog(owner);
        return vm.Confirmed;
    }

    /// <inheritdoc/>
    public async Task AlertAsync(string title, string message)
    {
        if (Owner is not { } owner) return;

        var vm = new ConfirmationViewModel(title, message, isAlert: true);
        await new ConfirmationWindow { DataContext = vm }.ShowDialog(owner);
    }

    /// <inheritdoc/>
    public Task ErrorAsync(string title, string message, Exception ex)
    {
        var details = ex.InnerException?.Message ?? ex.Message;
        return AlertAsync(title, $"{message}\n\nПричина: {details}");
    }

    /// <inheritdoc/>
    public async Task<Genre?> ShowGenreEditorAsync(Genre? genre = null)
    {
        if (Owner is not { } owner) return null;

        var vm = genre == null ? new AddGenreViewModel() : new AddGenreViewModel(genre);
        await new AddGenreWindow { DataContext = vm }.ShowDialog(owner);
        return vm.Confirmed ? vm.BuildGenre() : null;
    }

    /// <inheritdoc/>
    public async Task<Platform?> ShowPlatformEditorAsync(Platform? platform = null)
    {
        if (Owner is not { } owner) return null;

        var vm = platform == null ? new AddPlatformViewModel() : new AddPlatformViewModel(platform);
        await new AddPlatformWindow { DataContext = vm }.ShowDialog(owner);
        return vm.Confirmed ? vm.BuildPlatform() : null;
    }

    /// <inheritdoc/>
    public async Task<Tag?> ShowTagEditorAsync(Tag? tag = null)
    {
        if (Owner is not { } owner) return null;

        var vm = tag == null ? new AddTagViewModel() : new AddTagViewModel(tag);
        await new AddTagWindow { DataContext = vm }.ShowDialog(owner);
        return vm.Confirmed ? vm.BuildTag() : null;
    }
}