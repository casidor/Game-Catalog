using Game_Catalog.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Handles saving and loading application settings to and from a JSON file.
    /// </summary>
    public static class SettingsService
    {
        /// <summary>Path to settings.json in the user's AppData folder.</summary>
        public static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "settings.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        /// <summary>The current in-memory settings instance.</summary>
        public static AppSettings Current { get; private set; } = new();

        /// <summary>Raised when the disk capacity value changes.</summary>
        public static event Action? DiskCapacityChanged;

        /// <summary>Saves the current settings to disk.</summary>
        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, Options));
        }

        /// <summary>Loads settings from disk, or keeps defaults if the file does not exist.</summary>
        public static void Load()
        {
            if (!File.Exists(SettingsPath)) return;
            var loaded = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(SettingsPath), Options);
            if (loaded != null) Current = loaded;
        }

        /// <summary>Updates the disk capacity, saves, and notifies subscribers.</summary>
        public static void UpdateDiskCapacity(double gb)
        {
            Current.DiskCapacityGB = gb;
            Save();
            DiskCapacityChanged?.Invoke();
        }
    }
}