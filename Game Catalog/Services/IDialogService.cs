using Game_Catalog.Models;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.Services;

/// <summary> Abstraction over dialog windows so ViewModels do not depend on UI types. </summary>
public interface IDialogService
{
    /// <summary> Shows a two-button dialog and returns true if the user confirmed. </summary>
    Task<bool> ConfirmAsync(string title, string message,
        string confirmText, string cancelText = "Скасувати");

    /// <summary> Shows a single-button informational dialog. </summary>
    Task AlertAsync(string title, string message);

    /// <summary> Shows a single-button error dialog with the exception reason. </summary>
    Task ErrorAsync(string title, string message, Exception ex);

    /// <summary> Shows the genre editor and returns the entered genre, or null if cancelled. </summary>
    Task<Genre?> ShowGenreEditorAsync(Genre? genre = null);

    /// <summary> Shows the platform editor and returns the entered platform, or null if cancelled. </summary>
    Task<Platform?> ShowPlatformEditorAsync(Platform? platform = null);

    /// <summary> Shows the tag editor and returns the entered tag, or null if cancelled. </summary>
    Task<Tag?> ShowTagEditorAsync(Tag? tag = null);

    /// <summary> Shows the studio editor and returns the entered studio, or null if cancelled. </summary>
    Task<Studio?> ShowStudioEditorAsync(Studio? studio = null);

    /// <summary> Lets the user pick an image file, copies it to the covers folder and returns the copy path, or null if cancelled or unusable. </summary>
    Task<string?> PickImageAsync(string title);

    /// <summary> Asks for an image address, downloads it into the covers folder and returns the file path, or null if cancelled. </summary>
    Task<string?> DownloadImageAsync();

    /// <summary> Shows the game editor. The editor saves the game itself; returns the saved game, or null if cancelled. </summary>
    Task<Game?> ShowGameEditorAsync(Game? game = null);
}