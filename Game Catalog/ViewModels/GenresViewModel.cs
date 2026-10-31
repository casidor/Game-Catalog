using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the genres tab of the dictionaries page. </summary>
    public sealed partial class GenresViewModel : DictionaryTabViewModel<Genre>
    {
        protected override DictionaryTexts Texts { get; } = new(
            LoadError: "Помилка завантаження жанрів",
            SaveError: "Помилка збереження жанру",
            DeleteError: "Помилка видалення жанру",
            DeleteTitle: "Видалення жанру",
            DeleteMessage: name => $"Видалити жанр «{name}»? Цю дію не можна скасувати.",
            BlockedTitle: "Неможливо видалити жанр",
            BlockedMessage: name => $"Жанр «{name}» використовується в іграх або студіях.\n" +
                                    "Спершу відв'яжіть його від них.");

        protected override List<Genre> LoadItems(string search) => GenreRepository.GetAll(search);

        protected override Task<Genre?> ShowEditorAsync(Genre? item) =>
            Dialogs.Current.ShowGenreEditorAsync(item);

        protected override void InsertItem(Genre item) => GenreRepository.Insert(item);

        protected override void UpdateItem(Genre item) => GenreRepository.Update(item);

        protected override bool DeleteItem(Genre item) => GenreRepository.Delete(item.GenreId);

        protected override string GetName(Genre item) => item.GenreName;
    }
}