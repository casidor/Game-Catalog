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
}