using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Game_Catalog.Views;

/// <summary> Avalonia implementation of <see cref="IDialogService"/> based on the main window. </summary>
public sealed class DialogService : IDialogService
{
    private static Window? Owner
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;
            return desktop.Windows.LastOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }
    }

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

    /// <inheritdoc/>
    public async Task<Studio?> ShowStudioEditorAsync(Studio? studio = null)
    {
        if (Owner is not { } owner) return null;

        var vm = studio == null ? new AddStudioViewModel() : new AddStudioViewModel(studio);
        await new AddStudioWindow { DataContext = vm }.ShowDialog(owner);
        return vm.Confirmed ? vm.BuildStudio() : null;
    }

    /// <inheritdoc/>
    public async Task<string?> PickImageAsync(string title)
    {
        if (Owner is not { } owner) return null;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new("Зображення") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"] }]
        });
        if (files.Count == 0) return null;

        var source = files[0].Path.LocalPath;
        if (!ImageInspector.IsReadable(source))
        {
            await AlertAsync("Файл не підтримується",
                "Не вдалося прочитати зображення. Оберіть JPG, PNG або WEBP.");
            return null;
        }

        try
        {
            var dest = MediaStore.NewFilePath(Path.GetExtension(source));
            File.Copy(source, dest, overwrite: true);
            return dest;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ErrorAsync("Не вдалося скопіювати зображення", "Файл не збережено.", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadImageAsync()
    {
        if (Owner is not { } owner) return null;

        string? downloaded = null;
        var dialog = new InputWindow(
            title: "Завантажити за URL",
            prompt: "Адреса зображення",
            placeholder: "https://...",
            confirmText: "Завантажити",
            handler: async url =>
            {
                try
                {
                    downloaded = await ImageDownloader.DownloadAsync(url);
                    return null;
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message;
                }
            });

        await dialog.ShowDialog(owner);
        return downloaded;
    }

    /// <inheritdoc/>
    public async Task<Game?> ShowGameEditorAsync(Game? game = null)
    {
        if (Owner is not { } owner) return null;

        var vm = game == null ? new AddGameViewModel() : new AddGameViewModel(game);
        await new AddGameWindow { DataContext = vm }.ShowDialog(owner);
        return vm.SavedGame;
    }

    /// <inheritdoc/>
    public async Task<PlaySession?> ShowSessionEditorAsync(PlaySession? session = null, int? presetGameId = null)
    {
        if (Owner is not { } owner) return null;

        var vm = new SessionEditorViewModel(session, presetGameId);
        await new SessionEditorWindow { DataContext = vm }.ShowDialog(owner);
        return vm.SavedSession;
    }
}