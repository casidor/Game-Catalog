using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Models
{
    /// <summary> One palette entry with a variant tuned for each theme. An empty key means no color. </summary>
    public sealed record TagColor(string Key, string Name, string DarkHex, string LightHex)
    {
        /// <summary> True for the "no color" entry. </summary>
        public bool IsNone => Key.Length == 0;
    }

    /// <summary> Fixed palette of tag colors. The keys are what the database stores. </summary>
    public static class TagPalette
    {
        /// <summary> The "no color" entry, stored as NULL. </summary>
        public static TagColor NoColor { get; } = new("", "Без кольору", "", "");

        /// <summary> Real palette colors in display order. </summary>
        public static IReadOnlyList<TagColor> Colors { get; } = new TagColor[]
        {
            new("Red",     "Червоний",    "#e57f7f", "#c24b4b"),
            new("Orange",  "Оранжевий",   "#e8956a", "#c9622d"),
            new("Amber",   "Бурштиновий", "#e3b25a", "#b9791c"),
            new("Yellow",  "Жовтий",      "#e0c75c", "#a88b17"),
            new("Lime",    "Лаймовий",    "#a8c969", "#6f9a2a"),
            new("Green",   "Зелений",     "#6fbf8a", "#3d8f5b"),
            new("Teal",    "Бірюзовий",   "#5fbfb0", "#2f8f84"),
            new("Cyan",    "Блакитний",   "#5fb8cc", "#2a8499"),
            new("Sky",     "Небесний",    "#6aaee0", "#2f7fb8"),
            new("Blue",    "Синій",       "#7a9fe0", "#3f66bf"),
            new("Indigo",  "Індиго",      "#8e8fe0", "#5557b8"),
            new("Violet",  "Фіолетовий",  "#a98ae0", "#7a52b8"),
            new("Fuchsia", "Фуксія",      "#cf86d6", "#a04aa8"),
            new("Pink",    "Рожевий",     "#e387b3", "#bf4f84"),
            new("Brown",   "Коричневий",  "#b38b6a", "#85593a"),
            new("Olive",   "Оливковий",   "#a3a56a", "#6f7130"),
            new("Gray",    "Сірий",       "#9aa0a8", "#6b7280")
        };

        /// <summary> All dialog options: "no color" first, then the colors (18 cells). </summary>
        public static IReadOnlyList<TagColor> Options { get; } =
            new[] { NoColor }.Concat(Colors).ToArray();

        /// <summary> Finds a palette color by key, or null for an empty or unknown key. </summary>
        public static TagColor? Find(string? key) =>
            Colors.FirstOrDefault(c => c.Key == key);

        /// <summary> Color used when the stored key is not in the palette. </summary>
        public static TagColor Fallback => Find("Gray")!;
    }
}