namespace Game_Catalog.Models
{
    /// <summary> Overall state of the library: standalone games that are not archived. </summary>
    public class LibrarySummary
    {
        /// <summary> Number of games in the library. </summary>
        public int GameCount { get; set; }

        /// <summary> Total play time in hours, counted over finished sessions only. </summary>
        public double TotalHours { get; set; }

        /// <summary> Total disk size of installed games in gigabytes. Null when no game has a measured size. </summary>
        public double? TotalSizeGb { get; set; }

        /// <summary> Average personal rating from 1 to 10. Null when no game is rated. </summary>
        public double? AverageRating { get; set; }
    }
}
