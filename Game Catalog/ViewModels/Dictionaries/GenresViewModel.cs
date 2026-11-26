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
            SearchError: "Помилка пошуку жанрів",
            SaveError: "Помилка збереження жанру",
            DeleteError: "Помилка видалення жанрів",
            SearchPlaceholder: "🔍 Пошук жанру...",
            AddTip: "Додати жанр",
            ItemOne: "жанр",
            ItemFew: "жанри",
            ItemMany: "жанрів",
            DeleteTitleOne: "Видалення жанру",
            DeleteTitleMany: "Видалення жанрів",
            BlockedOne: name => $"Жанр «{name}» використовується в іграх або студіях.\n" +
                                "Спершу відв'яжіть його від них.",
            BlockedManyIntro: "Усі вибрані жанри використовуються в іграх або студіях:",
            BlockedManyHint: "Спершу відв'яжіть їх від цих ігор і студій.",
            SkippedIntro: "Ці жанри використовуються в іграх або студіях, тому їх буде пропущено:");

        protected override List<Genre> LoadItems(DictionarySortField field, bool descending) =>
            GenreRepository.GetAll(descending);

        protected override HashSet<int> LoadMatchIds(string search) => GenreRepository.GetMatchIds(search);

        protected override HashSet<int> LoadIdsInUse(IReadOnlyCollection<int> ids) => GenreRepository.GetIdsInUse(ids);

        protected override int GetId(Genre item) => item.GenreId;

        protected override DictionaryRow<Genre> CreateRow(Genre item) => new GenreRow(item);

        protected override Task<Genre?> ShowEditorAsync(Genre? item) =>
            Dialogs.Current.ShowGenreEditorAsync(item);

        protected override void InsertItem(Genre item) => GenreRepository.Insert(item);

        protected override void UpdateItem(Genre item) => GenreRepository.Update(item);

        protected override void DeleteItems(IReadOnlyCollection<int> ids) => GenreRepository.Delete(ids);

        protected override string GetName(Genre item) => item.GenreName;
    }
}
