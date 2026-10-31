using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the genres tab of the reference books page. </summary>
    public partial class GenresViewModel : ViewModelBase
    {
        /// <summary> Genres matching the current search, ordered by name. </summary>
        public ObservableCollection<Genre> Genres { get; } = new();

        /// <summary> Text used to search genres by name. </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary> True when there are no genres at all. </summary>
        public bool IsEmpty => Genres.Count == 0 && string.IsNullOrWhiteSpace(SearchText);

        /// <summary> True when the search found nothing. </summary>
        public bool IsFilteredEmpty => Genres.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

        partial void OnSearchTextChanged(string value) => Reload();

        /// <summary> Reloads the genre list from the database using the current search. </summary>
        public void Reload()
        {
            try
            {
                var items = GenreRepository.GetAll(SearchText);
                Genres.Clear();
                foreach (var g in items)
                    Genres.Add(g);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження жанрів", ex);
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
        }

        /// <summary> Opens the editor and saves a new genre. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var genre = await Dialogs.Current.ShowGenreEditorAsync();
            if (genre == null) return;

            try
            {
                GenreRepository.Insert(genre);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження жанру", ex);
                return;
            }
            Reload();
        }

        /// <summary> Opens the editor for the given genre and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync(Genre genre)
        {
            var edited = await Dialogs.Current.ShowGenreEditorAsync(genre);
            if (edited == null) return;

            try
            {
                GenreRepository.Update(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження жанру", ex);
                return;
            }
            Reload();
        }

        /// <summary> Deletes the genre after confirmation unless it is still in use. </summary>
        [RelayCommand]
        private async Task DeleteAsync(Genre genre)
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення жанру",
                $"Видалити жанр «{genre.GenreName}»? Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            bool deleted;
            try
            {
                deleted = GenreRepository.Delete(genre.GenreId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення жанру", ex);
                return;
            }

            if (!deleted)
            {
                await Dialogs.Current.AlertAsync(
                    "Неможливо видалити жанр",
                    $"Жанр «{genre.GenreName}» використовується в іграх або студіях.\n" +
                    "Спершу відв'яжіть його від них.");
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