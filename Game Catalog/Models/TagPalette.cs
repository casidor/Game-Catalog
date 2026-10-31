using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Models
{
    /// <summary> One color of the tag palette with a variant tuned for each theme. </summary>
    public sealed record TagColor(string Key, string Name, string DarkHex, string LightHex);

    /// <summary> Fixed palette of tag colors. The keys are what the database stores. </summary>
    public static class TagPalette
    {
        /// <summary> All palette colors in display order (4 per row). </summary>
        public static IReadOnlyList<TagColor> Colors { get; } = new TagColor[]
        {
            new("Red",     "Червоний",    "#f87171", "#dc2626"),
            new("Orange",  "Оранжевий",   "#fb923c", "#ea580c"),
            new("Amber",   "Бурштиновий", "#fbbf24", "#d97706"),
            new("Yellow",  "Жовтий",      "#facc15", "#ca8a04"),
            new("Lime",    "Лаймовий",    "#a3e635", "#65a30d"),
            new("Green",   "Зелений",     "#4ade80", "#16a34a"),
            new("Teal",    "Бірюзовий",   "#2dd4bf", "#0d9488"),
            new("Cyan",    "Блакитний",   "#22d3ee", "#0891b2"),
            new("Sky",     "Небесний",    "#38bdf8", "#0284c7"),
            new("Blue",    "Синій",       "#60a5fa", "#2563eb"),
            new("Indigo",  "Індиго",      "#818cf8", "#4f46e5"),
            new("Violet",  "Фіолетовий",  "#a78bfa", "#7c3aed"),
            new("Fuchsia", "Фуксія",      "#e879f9", "#c026d3"),
            new("Pink",    "Рожевий",     "#f472b6", "#db2777"),
            new("Brown",   "Коричневий",  "#b4835a", "#92400e"),
            new("Gray",    "Сірий",       "#9ca3af", "#6b7280")
        };

        /// <summary> Finds a palette color by key, or null for an empty or unknown key. </summary>
        public static TagColor? Find(string? key) =>
            Colors.FirstOrDefault(c => c.Key == key);

        /// <summary> Color used when the stored key is not in the palette. </summary>
        public static TagColor Fallback => Find("Gray")!;
    }
}