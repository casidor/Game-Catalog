namespace Game_Catalog.Models
{
    /// <summary> Number of goals of games that are not archived, in total and by status. </summary>
    public class GoalSummary
    {
        /// <summary> Number of all goals. </summary>
        public int TotalCount { get; set; }

        /// <summary> Active goals whose due date has not passed. </summary>
        public int ActiveCount { get; set; }

        /// <summary> Active goals past their due date. </summary>
        public int OverdueCount { get; set; }

        /// <summary> Completed goals. </summary>
        public int CompletedCount { get; set; }

        /// <summary> Cancelled goals. </summary>
        public int CancelledCount { get; set; }
    }
}
