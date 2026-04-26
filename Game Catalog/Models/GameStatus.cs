using System;
using System.Collections.Generic;
using System.Text;

namespace Game_Catalog.Models
{
    /// <summary>
    /// Represents the current playing status of a game in the catalog.
    /// </summary>
    public enum GameStatus
    {
        NotInstalled,
        InProgress,
        Completed,
        Abandoned
    }
}
