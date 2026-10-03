using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Game_Catalog.Models
{
    /// <summary>
    /// Shared application state that holds all data collections.
    /// Implements the Singleton pattern to ensure a single source of truth.
    /// </summary>
    public class AppData
    {
        /// <summary>
        /// Singleton instance of the application data.
        /// </summary>
        public static AppData Instance { get; } = new();

        /// <summary>
        /// Collection of all games in the library.
        /// </summary>
        public ObservableCollection<Game> Games { get; } = new();

        /// <summary>
        /// Collection of all game development studios.
        /// </summary>
        public ObservableCollection<Studio> Studios { get; } = new();
        
        
        /// <summary>
        /// Collection of all play sessions.
        /// </summary>
        public ObservableCollection<PlaySession> Sessions { get; } = new();

        /// <summary> Games that are not archived. Use this for statistics and disk checks. </summary>
        public IEnumerable<Game> ActiveGames => Games.Where(g => !g.IsArchived);

        /// <summary> Removes a game and all its DLC and play sessions from the application data. </summary>
        public void RemoveGameCascade(Game game)
        {
            var ids = Games
                .Where(g => g.ParentGameId == game.Id)
                .Select(g => g.Id)
                .Append(game.Id)
                .ToHashSet();

            foreach (var s in Sessions.Where(s => ids.Contains(s.GameId)).ToList())
                Sessions.Remove(s);

            foreach (var g in Games.Where(g => ids.Contains(g.Id)).ToList())
                Games.Remove(g);
        }

        private AppData() { }
    }
}
