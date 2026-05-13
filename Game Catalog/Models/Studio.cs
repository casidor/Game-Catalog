using System;

namespace Game_Catalog.Models
{
    /// <summary> Represents a video game development studio. </summary>
    public class Studio
    {
        /// <summary> Unique identifier for the studio. </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _name = string.Empty;
        /// <summary> Name of the studio. </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        private string _country = string.Empty;
        /// <summary> Country where the studio is based. </summary>
        public string Country
        {
            get => _country;
            set => _country = value ?? string.Empty;
        }

        private int _foundationYear;
        /// <summary> The year the studio was founded. </summary>
        public int FoundationYear
        {
            get => _foundationYear;
            set => _foundationYear = Math.Max(0, value);
        }

        private string _mainGenre = string.Empty;
        /// <summary> The main genre the studio is known for. </summary>
        public string MainGenre
        {
            get => _mainGenre;
            set => _mainGenre = value ?? string.Empty;
        }
    }
}