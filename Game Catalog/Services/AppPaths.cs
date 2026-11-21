using System;
using System.IO;

namespace Game_Catalog.Services
{
    /// <summary> Single place that decides where the application keeps its data on this machine. </summary>
    public static class AppPaths
    {
        /// <summary> Root folder of the database, settings and media. Resolved per OS (AppData on Windows, ~/.config on Linux). </summary>
        public static readonly string Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog");
    }
}
