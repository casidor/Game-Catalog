using System;
using System.Collections.Generic;

namespace Game_Catalog.Models
{
    /// <summary> Filter criteria and sort order for the goal list. Empty lists and null bounds mean "not set". </summary>
    public sealed record GoalFilter
    {
        /// <summary> Field the list is ordered by. Not a criterion. </summary>
        public GoalSortField SortField { get; init; } = GoalSortField.Priority;

        /// <summary> True for descending order; by default the highest priority (1) goes first. Not a criterion. </summary>
        public bool SortDescending { get; init; }

        /// <summary> Games to keep. Empty means any game. </summary>
        public IReadOnlyList<int> GameIds { get; init; } = Array.Empty<int>();

        /// <summary> Priorities to keep. Empty means any priority. </summary>
        public IReadOnlyList<int> Priorities { get; init; } = Array.Empty<int>();

        /// <summary> Statuses to keep, Overdue included. Empty means any status. </summary>
        public IReadOnlyList<GoalStatus> Statuses { get; init; } = Array.Empty<GoalStatus>();

        /// <summary> First day of the due date range, inclusive. Goals without a due date never match a bound. </summary>
        public DateTime? DueFrom { get; init; }

        /// <summary> Last day of the due date range, inclusive. </summary>
        public DateTime? DueTo { get; init; }

        /// <summary> True when no criterion is set. </summary>
        public bool IsEmpty =>
            GameIds.Count == 0 && Priorities.Count == 0 && Statuses.Count == 0 &&
            DueFrom == null && DueTo == null;
    }
}