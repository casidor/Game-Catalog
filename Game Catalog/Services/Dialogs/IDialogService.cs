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

    /// <summary>
    /// Lets the user pick an image file and returns its absolute path, or null if cancelled or unusable.
    /// The file is not copied; it is imported into the media folder when the game is saved.
    /// </summary>
    /// <param name="title"> Title of the picker. </param>
    /// <param name="startFolder"> Folder to open if it exists; otherwise the Downloads folder is opened. </param>
    Task<string?> PickImageAsync(string title, string? startFolder = null);

    /// <summary> Asks for an image address, downloads it into the temporary folder and returns the file path, or null if cancelled. </summary>
    Task<string?> DownloadImageAsync();

    /// <summary> Opens the folder in the file manager of the OS, creating it if needed. Shows an error if it cannot be opened. </summary>
    Task OpenFolderAsync(string path);

    /// <summary> Opens an existing folder in the file manager without creating it. Shows a message if it is missing. </summary>
    Task OpenExistingFolderAsync(string path);

    /// <summary> Lets the user pick a folder and returns its absolute path, or null if cancelled. </summary>
    /// <param name="title"> Title of the picker. </param>
    /// <param name="startFolder"> Folder to open if it exists; otherwise the system default. </param>
    Task<string?> PickFolderAsync(string title, string? startFolder = null);

    /// <summary>
    /// Lets the user pick the launch file of a game and returns its absolute path, or null if cancelled.
    /// Windows shows programs and shortcuts; other OSes show all files, since programs there have no extension.
    /// </summary>
    /// <param name="startFolder"> Folder to open if it exists, usually the install folder; otherwise the system default. </param>
    Task<string?> PickLaunchFileAsync(string? startFolder = null);

    /// <summary> Shows the game editor. The editor saves the game itself; returns the saved game, or null if cancelled. </summary>
    Task<Game?> ShowGameEditorAsync(Game? game = null);

    /// <summary> Shows the session editor. The editor saves the session itself; returns the saved session, or null if cancelled. </summary>
    /// <param name="session"> Session to edit, or null to add a new one. </param>
    /// <param name="presetGameId"> Game preselected for a new session, or null. </param>
    Task<PlaySession?> ShowSessionEditorAsync(PlaySession? session = null, int? presetGameId = null);

    /// <summary> Shows the goal editor. The editor saves the goal itself; returns the saved goal, or null if cancelled. </summary>
    /// <param name="goal"> Goal to edit, or null to add a new one. </param>
    /// <param name="presetGameId"> Game preselected for a new goal, or null. </param>
    Task<Goal?> ShowGoalEditorAsync(Goal? goal = null, int? presetGameId = null);
}