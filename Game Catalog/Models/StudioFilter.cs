namespace Game_Catalog.Models
{
    /// <summary> Sort order of the studio list. Values are used directly in SQL. </summary>
    public enum StudioSort
    {
        NameAsc = 1,
        NameDesc = 2,
        YearAsc = 3,
        YearDesc = 4
    }

    /// <summary> Search, filter and sort criteria for the studio list. Null or empty means "not set". </summary>
    public sealed record StudioFilter
    {
        /// <summary> Text searched in the studio name and country. </summary>
        public string? Search { get; init; }

        /// <summary> Exact country to match. </summary>
        public string? Country { get; init; }

        /// <summary> Main genre id to match. </summary>
        public int? MainGenreId { get; init; }

        /// <summary> Lower bound of the foundation year, inclusive. </summary>
        public int? YearFrom { get; init; }

        /// <summary> Upper bound of the foundation year, inclusive. </summary>
        public int? YearTo { get; init; }

        /// <summary> Sort order of the result. </summary>
        public StudioSort SortBy { get; init; } = StudioSort.NameAsc;
    }
}