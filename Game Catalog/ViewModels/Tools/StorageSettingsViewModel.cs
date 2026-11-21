using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Storage tab of the settings: size of the media folder and removal of unused files.
    /// Counting is started by the user only, because scanning the folder may take time.
    /// </summary>
    public partial class StorageSettingsViewModel : ViewModelBase
    {
        /// <summary> Size and file count of the media folder, or null until counted. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCounted), nameof(CountButtonText))]
        private string? _usageText;

        /// <summary> Size and file count of unused files, or null until counted. </summary>
        [ObservableProperty]
        private string? _unusedText;

        /// <summary> Number of unused files found by the last count. </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CleanUnusedCommand))]
        private int _unusedCount;

        /// <summary> True after a successful count. </summary>
        public bool IsCounted => UsageText != null;

        public string CountButtonText => IsCounted ? "Перерахувати" : "Підрахувати";

        /// <summary> Counts the media folder. </summary>
        [RelayCommand]
        private Task CountAsync() => ScanAsync();

        /// <summary> Opens the media folder in the file manager of the OS. </summary>
        [RelayCommand]
        private Task OpenFolderAsync() => Dialogs.Current.OpenFolderAsync(MediaStore.Folder);

        /// <summary>
        /// Removes files no record refers to, after confirmation.
        /// The list is built again right before deleting, not taken from the numbers on the screen.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCleanUnused))]
        private async Task CleanUnusedAsync()
        {
            var unused = await ScanAsync();
            if (unused == null || unused.Count == 0) return;

            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Очищення медіа",
                "Видалити невикористані файли?\n" +
                $"Файлів: {unused.Count}, загальний розмір: {FormatSize(unused.Sum(f => f.Bytes))}.\n" +
                "Будуть видалені лише файли, не пов'язані з жодною грою. Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            var failed = MediaStore.DeleteFiles(unused.Select(f => f.Path));
            await ScanAsync();

            if (failed > 0)
                await Dialogs.Current.AlertAsync("Очищення медіа",
                    $"Не вдалося видалити файлів: {failed}. Можливо, вони відкриті в іншій програмі.");
        }

        private bool CanCleanUnused() => UnusedCount > 0;

        /// <summary>
        /// Counts all media files and the unused ones and updates the texts.
        /// Returns the unused files, or null on failure: then nothing is shown and nothing can be removed.
        /// </summary>
        private async Task<List<MediaStore.MediaFile>?> ScanAsync()
        {
            try
            {
                var files = MediaStore.GetFiles();
                var unused = MediaStore.FindUnused(files, MediaRepository.GetReferencedPaths());

                UsageText = $"Займає: {FormatSize(files.Sum(f => f.Bytes))} · файлів: {files.Count}";
                UnusedText = unused.Count == 0
                    ? "Невикористаних файлів немає"
                    : $"Невикористані: {FormatSize(unused.Sum(f => f.Bytes))} · файлів: {unused.Count}";
                UnusedCount = unused.Count;
                return unused;
            }
            catch (DatabaseException ex)
            {
                Reset();
                await ShowErrorAsync("Помилка підрахунку медіа", ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Reset();
                await Dialogs.Current.ErrorAsync("Помилка підрахунку медіа", "Не вдалося прочитати теку медіа.", ex);
            }
            return null;
        }

        private void Reset()
        {
            UsageText = null;
            UnusedText = null;
            UnusedCount = 0;
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024L * 1024 => $"{bytes / 1024.0:0.0} КБ",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.0} МБ",
            _ => $"{bytes / (1024.0 * 1024 * 1024):0.00} ГБ"
        };

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
