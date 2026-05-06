using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Handles saving and loading application data to and from JSON files.
    /// </summary>
    public static class DataService
    {
        /// <summary>
        /// Default save path in the user's AppData folder.
        /// </summary>
        public static readonly string DefaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "data.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        private sealed record Snapshot(List<Studio> Studios, List<Game> Games, List<Game> ArchivedGames);

        /// <summary>
        /// Saves all application data to the specified path, or the default path if not provided.
        /// </summary>
        public static void Save(string? path = null)
        {
            path ??= DefaultPath;
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

        /// <summary>
        /// Loads application data from the specified JSON file into AppData.
        /// </summary>
        public static void Load(string path)
        {
            if (!File.Exists(path)) return;

            var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), Options);
            if (snapshot == null) return;

            AppData.Instance.Studios.Clear();
            AppData.Instance.Games.Clear();
            AppData.Instance.ArchivedGames.Clear();

            foreach (var s in snapshot.Studios)
                AppData.Instance.Studios.Add(s);

            foreach (var g in snapshot.Games)
            {
                g.Developer = snapshot.Studios.FirstOrDefault(s => s.Id == g.DeveloperId);
                AppData.Instance.Games.Add(g);
            }

            foreach (var g in snapshot.ArchivedGames)
            {
                g.Developer = snapshot.Studios.FirstOrDefault(s => s.Id == g.DeveloperId);
                AppData.Instance.ArchivedGames.Add(g);
            }
        }

        /// <summary>
        /// Loads data from the default AppData path if it exists.
        /// </summary>
        public static void LoadDefault()
        {
            if (File.Exists(DefaultPath)) Load(DefaultPath);
        }
    }
}
