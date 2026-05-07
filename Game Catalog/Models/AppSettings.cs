namespace Game_Catalog.Models
{
    /// <summary>
    /// Stores user-configurable application settings persisted between sessions.
    /// </summary>
    public class AppSettings
    {
        /// <summary>The selected UI theme: "Dark" or "Light".</summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>The selected UI language: "UA" or "EN".</summary>
        public string Language { get; set; } = "UA";

        /// <summary>Total disk capacity in GB used for the statistics progress bar.</summary>
        public double DiskCapacityGB { get; set; } = 500;
    }
}
