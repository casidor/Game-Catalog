using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> Row of the genres table. </summary>
    public sealed class GenreRow : DictionaryRow<Genre>
    {
        /// <summary> Description or null if not specified, exposed for column binding. </summary>
        public string? Description =>
            string.IsNullOrWhiteSpace(Item.GenreDescription) ? null : Item.GenreDescription;

        public GenreRow(Genre genre) : base(genre) { }
    }
}
