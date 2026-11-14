using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the studio details page. </summary>
    public partial class StudioDetailsViewModel : ViewModelBase
    {
        /// <summary> The studio being displayed. </summary>
        public Studio Studio { get; private set; }

        /// <summary> Raised when the user requests to go back to the previous page. </summary>
        public event Action? BackRequested;

        /// <summary> Name of the studio. </summary>
        public string Name => Studio.StudioName;

        /// <summary> Country name or null if not specified. </summary>
        public string? DisplayCountry =>
            string.IsNullOrWhiteSpace(Studio.Country) ? null : Studio.Country;

        /// <summary> Foundation year or null if not specified. </summary>
        public string? DisplayFoundationYear => Studio.FoundationYear?.ToString();

        /// <summary> Main genre name or null if not specified. </summary>
        public string? DisplayMainGenre =>
            string.IsNullOrWhiteSpace(Studio.MainGenreName) ? null : Studio.MainGenreName;

        /// <summary> Website URL or null if not specified. </summary>
        public string? DisplayWebsite =>
            string.IsNullOrWhiteSpace(Studio.Website) ? null : Studio.Website;

        /// <summary> Indicates whether the studio has a website specified. </summary>
        public bool HasWebsite => !string.IsNullOrWhiteSpace(Studio.Website);

        public StudioDetailsViewModel(Studio studio)
        {
            Studio = studio;
        }

        /// <summary> Raises change notifications for everything shown on the page. </summary>
        private void RefreshAll()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(DisplayCountry));
            OnPropertyChanged(nameof(DisplayFoundationYear));
            OnPropertyChanged(nameof(DisplayMainGenre));
            OnPropertyChanged(nameof(DisplayWebsite));
            OnPropertyChanged(nameof(HasWebsite));
        }

        /// <summary> Reloads the studio from the database. Goes back if it no longer exists. </summary>
        private async Task ReloadAsync()
        {
            Studio? fresh;
            try
            {
                fresh = StudioRepository.GetById(Studio.StudioId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка завантаження студії", ex);
                return;
            }

            if (fresh == null)
            {
                BackRequested?.Invoke();
                return;
            }

            Studio = fresh;
            RefreshAll();
        }

        /// <summary> Opens the editor and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync()
        {
            var edited = await Dialogs.Current.ShowStudioEditorAsync(Studio);
            if (edited == null) return;

            try
            {
                StudioRepository.Update(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження студії", ex);
                return;
            }

            await ReloadAsync();
        }

        /// <summary> Deletes the studio after confirmation unless it still has games. </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення студії",
                $"Видалити студію «{Studio.StudioName}»? Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            bool deleted;
            try
            {
                deleted = StudioRepository.Delete(Studio.StudioId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студії", ex);
                return;
            }

            if (!deleted)
            {
                await Dialogs.Current.AlertAsync(
                    "Неможливо видалити студію",
                    $"Студія «{Studio.StudioName}» має ігри в бібліотеці або архіві.\n" +
                    "Спочатку видаліть або перепризначте ці ігри.");
                return;
            }

            BackRequested?.Invoke();
        }

        /// <summary> Opens the studio's website in the default web browser. </summary>
        [RelayCommand]
        private async Task OpenWebsiteAsync()
        {
            var url = Studio.Website.Trim();
            if (url.Length == 0) return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is { } window)
            {
                await window.Launcher.LaunchUriAsync(uri);
            }
        }

        /// <summary> Navigates back to the previous page. </summary>
        [RelayCommand]
        private void GoBack() => BackRequested?.Invoke();

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}