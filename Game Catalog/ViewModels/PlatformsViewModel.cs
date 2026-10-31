using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the platforms tab of the dictionaries page. </summary>
    public partial class PlatformsViewModel : ViewModelBase
    {
        /// <summary> Platforms matching the current search, ordered by name. </summary>
        public ObservableCollection<Platform> Platforms { get; } = new();

        /// <summary> Text used to search platforms by name or manufacturer. </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary> True when there are no platforms at all. </summary>
        public bool IsEmpty => Platforms.Count == 0 && string.IsNullOrWhiteSpace(SearchText);

        /// <summary> True when the search found nothing. </summary>
        public bool IsFilteredEmpty => Platforms.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

        partial void OnSearchTextChanged(string value) => Reload();

        /// <summary> Reloads the platform list from the database using the current search. </summary>
        public void Reload()
        {
            try
            {
                var items = PlatformRepository.GetAll(SearchText);
                Platforms.Clear();
                foreach (var p in items)
                    Platforms.Add(p);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження платформ", ex);
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
        }

        /// <summary> Opens the editor and saves a new platform. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var platform = await Dialogs.Current.ShowPlatformEditorAsync();
            if (platform == null) return;

            try
            {
                PlatformRepository.Insert(platform);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження платформи", ex);
                return;
            }
            Reload();
        }

        /// <summary> Opens the editor for the given platform and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync(Platform platform)
        {
            var edited = await Dialogs.Current.ShowPlatformEditorAsync(platform);
            if (edited == null) return;

            try
            {
                PlatformRepository.Update(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження платформи", ex);
                return;
            }
            Reload();
        }

        /// <summary> Deletes the platform after confirmation unless it is still in use. </summary>
        [RelayCommand]
        private async Task DeleteAsync(Platform platform)
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення платформи",
                $"Видалити платформу «{platform.PlatformName}»? Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            bool deleted;
            try
            {
                deleted = PlatformRepository.Delete(platform.PlatformId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення платформи", ex);
                return;
            }

            if (!deleted)
            {
                await Dialogs.Current.AlertAsync(
                    "Неможливо видалити платформу",
                    $"Платформа «{platform.PlatformName}» використовується в іграх.\n" +
                    "Спершу відв'яжіть її від них.");
                return;
            }
            Reload();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}