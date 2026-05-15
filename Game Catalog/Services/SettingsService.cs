using Game_Catalog.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Handles saving and loading application settings to and from a JSON file.
    /// Falls back to default settings if the file is missing or corrupted.
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

        /// <summary>
        /// True if the last Load completed successfully.
        /// False if the file was corrupted and defaults were used instead.
        /// </summary>
        public static bool LoadSucceeded { get; private set; } = false;

        /// <summary>Raised when a save operation fails. Provides a human-readable error message.</summary>
        public static event Action<string>? SaveFailed;

        /// <summary>Raised when the disk capacity value changes.</summary>
        public static event Action? DiskCapacityChanged;

        /// <summary>
        /// Saves the current settings to disk.
        /// Fires <see cref="SaveFailed"/> if the write operation fails.
        /// </summary>
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, Options));
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Loads settings from disk. Falls back to defaults if the file does not exist,
        /// is corrupted, or cannot be read. Sets <see cref="LoadSucceeded"/> accordingly.
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            try
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(SettingsPath), Options);

                if (loaded != null)
                {
                    Current = loaded;
                    LoadSucceeded = true;
                }
                else
                {
                    LoadSucceeded = false;
                }
            }
            catch
            {
                Current = new AppSettings();
                LoadSucceeded = false;
            }
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