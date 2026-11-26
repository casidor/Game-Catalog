using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the tags tab of the dictionaries page. </summary>
    public sealed class TagsViewModel : DictionaryTabViewModel<Tag>
    {
        protected override DictionaryTexts Texts { get; } = new(
            LoadError: "Помилка завантаження тегів",
            SearchError: "Помилка пошуку тегів",
            SaveError: "Помилка збереження тегу",
            DeleteError: "Помилка видалення тегів",
            SearchPlaceholder: "🔍 Пошук тегу...",
            AddTip: "Додати тег",
            ItemOne: "тег",
            ItemFew: "теги",
            ItemMany: "тегів",
            DeleteTitleOne: "Видалення тегу",
            DeleteTitleMany: "Видалення тегів",
            DeleteNote: "Зв'язки з іграми буде знято, самі ігри залишаться.");

        protected override List<Tag> LoadItems(DictionarySortField field, bool descending) =>
            TagRepository.GetAll(descending);

        protected override HashSet<int> LoadMatchIds(string search) => TagRepository.GetMatchIds(search);

        /// <summary> A tag is never in use in the blocking sense: deleting it only removes its links with games. </summary>
        protected override HashSet<int> LoadIdsInUse(IReadOnlyCollection<int> ids) => new();

        protected override int GetId(Tag item) => item.TagId;

        protected override Task<Tag?> ShowEditorAsync(Tag? item) =>
            Dialogs.Current.ShowTagEditorAsync(item);

        protected override void InsertItem(Tag item) => TagRepository.Insert(item);

        protected override void UpdateItem(Tag item) => TagRepository.Update(item);

        protected override void DeleteItems(IReadOnlyCollection<int> ids) => TagRepository.Delete(ids);

        protected override string GetName(Tag item) => item.TagName;
    }
}
