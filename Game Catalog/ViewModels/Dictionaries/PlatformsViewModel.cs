using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the platforms tab of the dictionaries page. </summary>
    public sealed class PlatformsViewModel : DictionaryTabViewModel<Platform>
    {
        protected override DictionaryTexts Texts { get; } = new(
            LoadError: "Помилка завантаження платформ",
            SearchError: "Помилка пошуку платформ",
            SaveError: "Помилка збереження платформи",
            DeleteError: "Помилка видалення платформи",
            DeleteTitle: "Видалення платформи",
            DeleteMessage: name => $"Видалити платформу «{name}»? Цю дію не можна скасувати.",
            BlockedTitle: "Неможливо видалити платформу",
            BlockedMessage: name => $"Платформа «{name}» використовується в іграх.\n" +
                                    "Спершу відв'яжіть її від них.");

        protected override List<Platform> LoadItems(DictionarySortField field, bool descending) =>
            PlatformRepository.GetAll(field, descending);

        protected override HashSet<int> LoadMatchIds(string search) => PlatformRepository.GetMatchIds(search);

        protected override int GetId(Platform item) => item.PlatformId;

        protected override DictionaryRow<Platform> CreateRow(Platform item) => new PlatformRow(item);

        protected override Task<Platform?> ShowEditorAsync(Platform? item) =>
            Dialogs.Current.ShowPlatformEditorAsync(item);

        protected override void InsertItem(Platform item) => PlatformRepository.Insert(item);

        protected override void UpdateItem(Platform item) => PlatformRepository.Update(item);

        protected override bool DeleteItem(Platform item) => PlatformRepository.Delete(item.PlatformId);

        protected override string GetName(Platform item) => item.PlatformName;
    }
}