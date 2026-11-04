using System;
using System.Collections.Generic;

namespace Game_Catalog.Models
{
    /// <summary> Filter criteria for the game list. Empty lists and null bounds mean "not set". </summary>
    public sealed record GameFilter
    {
        /// <summary> False for the library, true for the archive, null for both. </summary>
        public bool? IsArchived { get; init; }

        public IReadOnlyList<int> GenreIds { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> PlatformIds { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> TagIds { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> DeveloperIds { get; init; } = Array.Empty<int>();
        public IReadOnlyList<GameStatus> Statuses { get; init; } = Array.Empty<GameStatus>();

        public int? YearFrom { get; init; }
        public int? YearTo { get; init; }
        public int? RatingFrom { get; init; }
        public int? RatingTo { get; init; }
        public double? SizeFrom { get; init; }
        public double? SizeTo { get; init; }
        public double? HoursFrom { get; init; }
        public double? HoursTo { get; init; }

        /// <summary> True when no user criterion is set. The archive scope is not a criterion. </summary>
        public bool IsEmpty =>
            GenreIds.Count == 0 && PlatformIds.Count == 0 && TagIds.Count == 0 &&
            DeveloperIds.Count == 0 && Statuses.Count == 0 &&
            YearFrom == null && YearTo == null && RatingFrom == null && RatingTo == null &&
            SizeFrom == null && SizeTo == null && HoursFrom == null && HoursTo == null;
    }
}