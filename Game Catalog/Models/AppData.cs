using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// Collection of archived games.
        /// </summary>
        public ObservableCollection<Game> ArchivedGames { get; } = new();

        private AppData() { }
    }
}
