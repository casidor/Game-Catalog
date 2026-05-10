namespace Game_Catalog.Models
{
    /// <summary>
    /// Stores user-configurable application settings persisted between sessions.
    /// </summary>
    public class AppSettings
    {
        /// <summary>The selected UI theme: "Dark" or "Light".</summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>Total disk capacity in GB used for the statistics progress bar.</summary>
        public double DiskCapacityGB { get; set; } = 500;
        /// <summary>Indicates whether the app is launching for the first time.</summary>
        public bool IsFirstRun { get; set; } = true;

        /// <summary> RAWG API key used for game search and metadata fetch. Empty means offline mode. </summary>
        public string RawgApiKey { get; set; } = string.Empty;

    }
}
