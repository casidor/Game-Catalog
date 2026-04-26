using System;
using System.Collections.Generic;
using System.Text;

namespace Game_Catalog.Models
{
    /// <summary> Represents a video game development studio. </summary>
    public class Studio
    {
        /// <summary> Unique identifier for the studio. </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary> Name of the studio. </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary> Country where the studio is based. </summary>
        public string Country { get; set; } = string.Empty;
        /// <summary> The year the studio was founded. </summary>
        public int FoundationYear { get; set; }

        /// <summary> The main genre the studio is known for. </summary>
        public string MainGenre { get; set; } = string.Empty;
    }
}
