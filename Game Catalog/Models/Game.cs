using System;
using System.Text.Json.Serialization;

namespace Game_Catalog.Models
{
    /// <summary> Represents a video game in the user's catalog. </summary>
    public class Game
    {
        /// <summary> Unique identifier for the game. </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _title = string.Empty;
        /// <summary> Title of the video game. </summary>
        public string Title
        {
            get => _title;
            set => _title = value ?? string.Empty;
        }

        /// <summary> Identifier of the developer studio, used for JSON serialization. </summary>
        public Guid? DeveloperId { get; set; }

        /// <summary> Reference to the developer studio. Excluded from JSON serialization. </summary>
        [JsonIgnore]
        public Studio? Developer { get; set; }

        private string _genre = string.Empty;
        /// <summary> Genre of the game. </summary>
        public string Genre
        {
            get => _genre;
            set => _genre = value ?? string.Empty;
        }

        private int _releaseYear;
        /// <summary> The year the game was released. </summary>
        public int ReleaseYear
        {
            get => _releaseYear;
            set => _releaseYear = Math.Max(0, value);
        }

        private string _platform = string.Empty;
        /// <summary> The gaming platform (e.g., PC, PlayStation). </summary>
        public string Platform
        {
            get => _platform;
            set => _platform = value ?? string.Empty;
        }

        private double _sizeGB;
        /// <summary> Size of the game on disk in Gigabytes. </summary>
        public double SizeGB
        {
            get => _sizeGB;
            set => _sizeGB = Math.Max(0, value);
        }

        /// <summary> Current completion status of the game. </summary>
        public GameStatus Status { get; set; } = GameStatus.Planned;

        private double _hoursPlayed;
        /// <summary> Number of hours the user has played the game. </summary>
        public double HoursPlayed
        {
            get => _hoursPlayed;
            set => _hoursPlayed = Math.Max(0, value);
        }

        private int _personalRating = 5;
        /// <summary> Personal rating given by the user (1 to 10). </summary>
        public int PersonalRating
        {
            get => _personalRating;
            set => _personalRating = Math.Clamp(value, 1, 10);
        }

        private string _description = string.Empty;
        /// <summary> Short description of the game fetched from RAWG. </summary>
        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }

        private string _coverImagePath = string.Empty;
        /// <summary> Local file path to the downloaded cover image. </summary>
        public string CoverImagePath
        {
            get => _coverImagePath;
            set => _coverImagePath = value ?? string.Empty;
        }
    }
}