namespace Game_Catalog.Models
{
    /// <summary>
    /// Represents the current playing status of a game in the catalog.
    /// </summary>
    public enum GameStatus
    {
        /// <summary>Game is planned but not yet started.</summary>
        Planned,

        /// <summary>Game is currently being played.</summary>
        Playing,

        /// <summary>Game has been completed.</summary>
        Completed,

        /// <summary>Game was started but abandoned.</summary>
        Abandoned
    }
}