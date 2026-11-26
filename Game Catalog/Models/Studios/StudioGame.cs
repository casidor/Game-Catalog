namespace Game_Catalog.Models
{
    /// <summary> Game or DLC of a studio as listed on the studio details page. </summary>
    public class StudioGame
    {
        public int GameId { get; set; }

        /// <summary> Main game of a DLC; null for a standalone game. </summary>
        public int? ParentGameId { get; set; }

        public string GameTitle { get; set; } = string.Empty;

        public int? ReleaseYear { get; set; }

        public GameStatus GameStatus { get; set; }

        public int? PersonalRating { get; set; }

        /// <summary> True when the game is archived; for a DLC, when its main game is archived. </summary>
        public bool IsArchived { get; set; }

        /// <summary> Title of the main game of a DLC; null for a standalone game. </summary>
        public string? ParentTitle { get; set; }

        /// <summary> True for a DLC whose main game belongs to the same studio, so it is listed under that game. </summary>
        public bool IsNested { get; set; }
    }
}
