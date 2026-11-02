using System;
using System.Collections.Generic;

namespace Game_Catalog.Models
{
    /// <summary> Filter criteria for the studio list. Empty lists and null bounds mean "not set". </summary>
    public sealed record StudioFilter
    {
        /// <summary> Countries to keep. Empty means any country. </summary>
        public IReadOnlyList<string> Countries { get; init; } = Array.Empty<string>();

        /// <summary> Main genre ids to keep. Empty means any genre. </summary>
        public IReadOnlyList<int> GenreIds { get; init; } = Array.Empty<int>();

        /// <summary> Lower bound of the foundation year, inclusive. </summary>
        public int? YearFrom { get; init; }

        /// <summary> Upper bound of the foundation year, inclusive. </summary>
        public int? YearTo { get; init; }

        /// <summary> True when no criterion is set. </summary>
        public bool IsEmpty =>
            Countries.Count == 0 && GenreIds.Count == 0 && YearFrom == null && YearTo == null;
    }
}