namespace Game_Catalog.Models
{
    /// <summary>
    /// Stores user-configurable application settings persisted between sessions.
    /// </summary>
    public class AppSettings
    {
        /// <summary>The selected UI theme: "Dark" or "Light".</summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>True when the navigation sidebar shows captions next to the icons.</summary>
        public bool SidebarExpanded { get; set; }
    }
}
