namespace Game_Catalog.Models
{
    /// <summary> Row of the "games by play time" statistic. </summary>
    public class GamePlayTime
    {
        /// <summary> Identifier of the game. </summary>
        public int GameId { get; set; }

        private string _gameTitle = string.Empty;

        /// <summary> Title of the game. </summary>
        public string GameTitle
        {
            get => _gameTitle;
            set => _gameTitle = value ?? string.Empty;
        }

        /// <summary> Total play time of the game in hours, over finished sessions. </summary>
        public double TotalHours { get; set; }

        /// <summary> Share of the game in the total play time of the library, in percent. </summary>
        public double SharePercent { get; set; }
    }
}
