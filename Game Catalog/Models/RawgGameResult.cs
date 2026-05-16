using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Game_Catalog.Models
{
    /// <summary> Represents a single game entry from the RAWG search results. </summary>
    public class RawgGameResult
    {
        /// <summary> RAWG internal game identifier. </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary> Title of the game. </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary> Release date string in ISO format (yyyy-MM-dd). </summary>
        [JsonPropertyName("released")]
        public string? Released { get; set; }

        /// <summary> URL of the game's background cover image. </summary>
        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }

        /// <summary> List of genres associated with the game. </summary>
        [JsonPropertyName("genres")]
        public List<RawgGenre> Genres { get; set; } = new();
    }

    /// <summary> Represents a genre entry from RAWG. </summary>
    public class RawgGenre
    {
        /// <summary> Genre display name. </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary> Wraps the RAWG paginated search response. </summary>
    public class RawgSearchResponse
    {
        /// <summary> List of game results returned by the search. </summary>
        [JsonPropertyName("results")]
        public List<RawgGameResult> Results { get; set; } = new();
    }

    /// <summary> Extended game detail returned by the RAWG single-game endpoint. </summary>
    public class RawgGameDetail : RawgGameResult
    {
        /// <summary> Plain-text description of the game. </summary>
        [JsonPropertyName("description_raw")]
        public string? DescriptionRaw { get; set; }

        /// <summary> List of developer studios associated with the game. </summary>
        [JsonPropertyName("developers")]
        public List<RawgDeveloper> Developers { get; set; } = new();
    }

    /// <summary> Represents a developer studio entry from RAWG. </summary>
    public class RawgDeveloper
    {
        /// <summary> Studio display name. </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}