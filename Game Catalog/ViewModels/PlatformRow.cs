using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> Row of the platforms table. </summary>
    public sealed class PlatformRow : DictionaryRow<Platform>
    {
        /// <summary> Manufacturer or null if not specified, exposed for column binding. </summary>
        public string? Manufacturer =>
            string.IsNullOrWhiteSpace(Item.Manufacturer) ? null : Item.Manufacturer;

        public PlatformRow(Platform platform) : base(platform) { }
    }
}
