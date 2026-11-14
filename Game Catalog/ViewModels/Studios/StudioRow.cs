using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of the studios table: a studio plus the transient search-match flag. </summary>
    public partial class StudioRow : ObservableObject
    {
        /// <summary> The studio shown in this row. </summary>
        public Studio Studio { get; }

        /// <summary> Studio name, exposed for column binding and sorting. </summary>
        public string StudioName => Studio.StudioName;

        /// <summary> Country or null if not specified, exposed for column binding and sorting. </summary>
        public string? Country => string.IsNullOrWhiteSpace(Studio.Country) ? null : Studio.Country;

        /// <summary> Foundation year, exposed for column binding and sorting. </summary>
        public int? FoundationYear => Studio.FoundationYear;

        /// <summary> Main genre name or null if not specified, exposed for column binding and sorting. </summary>
        public string? MainGenreName =>
            string.IsNullOrWhiteSpace(Studio.MainGenreName) ? null : Studio.MainGenreName;

        /// <summary> True when the row matches the current search text. </summary>
        [ObservableProperty] private bool _isMatch;

        public StudioRow(Studio studio) => Studio = studio;
    }
}