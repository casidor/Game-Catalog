using System;
using System.Collections.Generic;

namespace Game_Catalog.Models
{
    /// <summary> Filter criteria for the session list. Empty lists and null bounds mean "not set". </summary>
    public sealed record SessionFilter
    {
        public IReadOnlyList<int> GameIds { get; init; } = Array.Empty<int>();

        /// <summary> First day of the range. A session belongs to the day it started. </summary>
        public DateTime? DateFrom { get; init; }

        /// <summary> Last day of the range, inclusive. </summary>
        public DateTime? DateTo { get; init; }

        /// <summary> Shortest accepted session length. Open sessions have no length and never match. </summary>
        public TimeSpan? DurationFrom { get; init; }

        /// <summary> Longest accepted session length. </summary>
        public TimeSpan? DurationTo { get; init; }

        public EntryMethod? Method { get; init; }

        /// <summary> True when no criterion is set. </summary>
        public bool IsEmpty =>
            GameIds.Count == 0 && DateFrom == null && DateTo == null &&
            DurationFrom == null && DurationTo == null && Method == null;
    }
}
