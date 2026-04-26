using System;
using System.Collections.Generic;
using System.Text;

namespace Game_Catalog.Models
{
    /// <summary> Represents a video game in the user's catalog. </summary>
    public class Game
    {
        /// <summary> Unique identifier for the game. </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary> Title of the video game. </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary> Reference to the studio that developed the game. </summary>
        public Studio? Developer { get; set; }

        /// <summary> Genre of the game. </summary>
        public string Genre { get; set; } = string.Empty;

        /// <summary> The year the game was released. </summary>
        public int ReleaseYear { get; set; }

        /// <summary> The gaming platform (e.g., PC, PlayStation). </summary>
        public string Platform { get; set; } = string.Empty;

        /// <summary> Size of the game on disk in Gigabytes. </summary>
        public double SizeGB { get; set; }

        /// <summary> Current completion status of the game. </summary>
        public GameStatus Status { get; set; } = GameStatus.NotInstalled;

        /// <summary> Number of hours the user has played the game. </summary>
        public double HoursPlayed { get; set; }

        /// <summary> Personal rating given by the user (1 to 10). </summary>
        public int PersonalRating { get; set; }
    }
}
