using System;

namespace Game_Catalog.Models
{
    /// <summary> The longest finished play session in the library. </summary>
    public class LongestSession
    {
        /// <summary> Identifier of the game the session belongs to. </summary>
        public int GameId { get; set; }

        private string _gameTitle = string.Empty;

        /// <summary> Title of the game. </summary>
        public string GameTitle
        {
            get => _gameTitle;
            set => _gameTitle = value ?? string.Empty;
        }

        /// <summary> Path to the cover of the game. Null when the game has no cover. </summary>
        public string? CoverImagePath { get; set; }

        /// <summary> Date and time the session started. </summary>
        public DateTime StartTime { get; set; }

        /// <summary> Duration of the session in seconds. </summary>
        public int DurationSeconds { get; set; }
    }
}
