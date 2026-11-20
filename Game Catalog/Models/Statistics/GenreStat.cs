namespace Game_Catalog.Models
{
    /// <summary> Row of the "games by genre" statistic. </summary>
    public class GenreStat
    {
        /// <summary> Identifier of the genre. </summary>
        public int GenreId { get; set; }

        private string _genreName = string.Empty;

        /// <summary> Name of the genre. </summary>
        public string GenreName
        {
            get => _genreName;
            set => _genreName = value ?? string.Empty;
        }

        /// <summary> Number of games of the genre in the library. </summary>
        public int GameCount { get; set; }

        /// <summary> Total play time in games of the genre, in hours. </summary>
        public double TotalHours { get; set; }
    }
}
