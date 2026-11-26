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
            DeleteError: "Помилка видалення платформ",
            SearchPlaceholder: "🔍 Пошук платформи...",
            AddTip: "Додати платформу",
            ItemOne: "платформу",
            ItemFew: "платформи",
            ItemMany: "платформ",
            DeleteTitleOne: "Видалення платформи",
            DeleteTitleMany: "Видалення платформ",
            BlockedOne: name => $"Платформа «{name}» використовується в іграх.\n" +
                                "Спершу відв'яжіть її від них.",
            BlockedManyIntro: "Усі вибрані платформи використовуються в іграх:",
            BlockedManyHint: "Спершу відв'яжіть їх від цих ігор.",
            SkippedIntro: "Ці платформи використовуються в іграх, тому їх буде пропущено:");

        protected override List<Platform> LoadItems(DictionarySortField field, bool descending) =>
            PlatformRepository.GetAll(field, descending);

        protected override HashSet<int> LoadMatchIds(string search) => PlatformRepository.GetMatchIds(search);

        protected override HashSet<int> LoadIdsInUse(IReadOnlyCollection<int> ids) => PlatformRepository.GetIdsInUse(ids);

        protected override int GetId(Platform item) => item.PlatformId;

        protected override DictionaryRow<Platform> CreateRow(Platform item) => new PlatformRow(item);

        protected override Task<Platform?> ShowEditorAsync(Platform? item) =>
            Dialogs.Current.ShowPlatformEditorAsync(item);

        protected override void InsertItem(Platform item) => PlatformRepository.Insert(item);

        protected override void UpdateItem(Platform item) => PlatformRepository.Update(item);

        protected override void DeleteItems(IReadOnlyCollection<int> ids) => PlatformRepository.Delete(ids);

        protected override string GetName(Platform item) => item.PlatformName;
    }
}
