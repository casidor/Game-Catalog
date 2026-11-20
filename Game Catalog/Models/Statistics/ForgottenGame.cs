using System;

namespace Game_Catalog.Models
{
    /// <summary> Row of the "forgotten games" statistic: a game kept in the library for a while and never played. </summary>
    public class ForgottenGame
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

        /// <summary> Date and time the game was added to the library. </summary>
        public DateTime AddedAt { get; set; }

        /// <summary> Full days since the game was added. </summary>
        public int DaysInLibrary { get; set; }
    }
}
