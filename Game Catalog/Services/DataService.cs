using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Handles saving and loading application data to and from JSON files.
    /// Implements the Singleton pattern to ensure a single source of truth.
    /// </summary>
    public static class DataService
    {
        /// <summary>Default save path in the user's AppData folder.</summary>
        public static readonly string DefaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "data.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        /// <summary>Suppresses auto-save while a Load operation is in progress.</summary>
        private static bool _isLoading;

        private sealed record Snapshot(List<Studio> Studios, List<Game> Games, List<Game> ArchivedGames);

        /// <summary>
        /// Raised when a save operation fails. Provides a human-readable error message.
        /// </summary>
        public static event Action<string>? SaveFailed;

        /// <summary>
        /// Saves all application data to the specified path, or the default path if not provided.
        /// Silently suppresses saves triggered during an active Load operation.
        /// </summary>
        public static void Save(string? path = null)
        {
            if (_isLoading) return;

            path ??= DefaultPath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                foreach (var g in AppData.Instance.Games)
                    g.DeveloperId = g.Developer?.Id;
                foreach (var g in AppData.Instance.ArchivedGames)
                    g.DeveloperId = g.Developer?.Id;

                var snapshot = new Snapshot(
                    AppData.Instance.Studios.ToList(),
                    AppData.Instance.Games.ToList(),
                    AppData.Instance.ArchivedGames.ToList());

                File.WriteAllText(path, JsonSerializer.Serialize(snapshot, Options));
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Subscribes to all data collections and automatically saves on any change.
        /// </summary>
        public static void EnableAutoSave()
        {
            AppData.Instance.Games.CollectionChanged += (_, _) => Save();
            AppData.Instance.ArchivedGames.CollectionChanged += (_, _) => Save();
            AppData.Instance.Studios.CollectionChanged += (_, _) => Save();
        }

        /// <summary>
        /// Loads application data from the specified JSON file into AppData.
        /// Returns true on success, false if the file is corrupted or unreadable.
        /// </summary>
        public static bool Load(string path)
        {
            if (!File.Exists(path)) return true;

            _isLoading = true;
            try
            {
                var json = File.ReadAllText(path);
                var snapshot = JsonSerializer.Deserialize<Snapshot>(json, Options);
                if (snapshot == null) return false;

                AppData.Instance.Studios.Clear();
                AppData.Instance.Games.Clear();
                AppData.Instance.ArchivedGames.Clear();

                foreach (var s in snapshot.Studios ?? [])
                    AppData.Instance.Studios.Add(s);

                foreach (var g in snapshot.Games ?? [])
                {
                    g.Developer = snapshot.Studios?.FirstOrDefault(s => s.Id == g.DeveloperId);
                    AppData.Instance.Games.Add(g);
                }

                foreach (var g in snapshot.ArchivedGames ?? [])
                {
                    g.Developer = snapshot.Studios?.FirstOrDefault(s => s.Id == g.DeveloperId);
                    AppData.Instance.ArchivedGames.Add(g);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Loads data from the default AppData path.
        /// Returns true on success, false if the file is corrupted or unreadable.
        /// </summary>
        public static bool LoadDefault() =>
            File.Exists(DefaultPath) ? Load(DefaultPath) : true;
    }
}