namespace Game_Catalog.Models
{
    /// <summary> Row of the "Game library" report: a standalone game that is not archived. </summary>
    public class LibraryReportRow
    {
        /// <summary> Title of the game. </summary>
        public string GameTitle { get; set; } = string.Empty;

        /// <summary> Name of the developer studio. </summary>
        public string DeveloperName { get; set; } = string.Empty;

        /// <summary> Genre names joined with commas, or null when the game has no genres. </summary>
        public string? Genres { get; set; }

        /// <summary> Platform names joined with commas, or null when the game has no platforms. </summary>
        public string? Platforms { get; set; }

        /// <summary> Release year, or null when unknown. </summary>
        public int? ReleaseYear { get; set; }

        /// <summary> Playing status of the game. </summary>
        public GameStatus GameStatus { get; set; }

        /// <summary> Personal rating from 1 to 10, or null when the game is not rated. </summary>
        public int? PersonalRating { get; set; }

        /// <summary> Total play time in hours, over finished sessions. </summary>
        public double TotalHours { get; set; }

        /// <summary> Disk size in gigabytes. Null when the game is not installed or its size is not measured. </summary>
        public double? SizeGb { get; set; }
    }
}
