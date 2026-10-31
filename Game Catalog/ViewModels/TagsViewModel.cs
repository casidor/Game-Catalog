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
            SaveError: "Помилка збереження тегу",
            DeleteError: "Помилка видалення тегу",
            DeleteTitle: "Видалення тегу",
            DeleteMessage: name => $"Видалити тег «{name}»? Його буде знято з усіх ігор, самі ігри залишаться.",
            BlockedTitle: "Неможливо видалити тег",
            BlockedMessage: name => $"Тег «{name}» неможливо видалити.");

        protected override List<Tag> LoadItems(string search) => TagRepository.GetAll(search);

        protected override Task<Tag?> ShowEditorAsync(Tag? item) =>
            Dialogs.Current.ShowTagEditorAsync(item);

        protected override void InsertItem(Tag item) => TagRepository.Insert(item);

        protected override void UpdateItem(Tag item) => TagRepository.Update(item);

        protected override bool DeleteItem(Tag item)
        {
            TagRepository.Delete(item.TagId);
            return true;
        }

        protected override string GetName(Tag item) => item.TagName;
    }
}