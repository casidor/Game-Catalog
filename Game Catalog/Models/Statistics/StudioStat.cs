namespace Game_Catalog.Models
{
    /// <summary> Row of the "games by studio" statistic. </summary>
    public class StudioStat
    {
        /// <summary> Identifier of the studio. </summary>
        public int StudioId { get; set; }

        private string _studioName = string.Empty;

        /// <summary> Name of the studio. </summary>
        public string StudioName
        {
            get => _studioName;
            set => _studioName = value ?? string.Empty;
        }

        /// <summary> Number of the studio's games in the library. </summary>
        public int GameCount { get; set; }

        /// <summary> Total play time in the studio's games, in hours. </summary>
        public double TotalHours { get; set; }

        /// <summary> Average rating of the studio's rated games. Null when none of them is rated. </summary>
        public double? AverageRating { get; set; }
    }
}
