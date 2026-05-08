using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Game_Catalog.Models
{
    /// <summary> Represents a video game in the user's catalog. </summary>
    public class Game
    {
        /// <summary> Unique identifier for the game. </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary> Title of the video game. </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary> Identifier of the developer studio, used for JSON serialization. </summary>
        public Guid? DeveloperId { get; set; }

        /// <summary> Reference to the developer studio. Excluded from JSON serialization. </summary>
        [JsonIgnore]
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

        /// <summary> Short description of the game fetched from RAWG. </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary> Local file path to the downloaded cover image. </summary>
        public string CoverImagePath { get; set; } = string.Empty;
    }
}
