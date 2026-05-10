using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Game_Catalog.Services
{
    /// <summary> Provides access to the RAWG API for game search, detail fetch, and cover download. </summary>
    public static class RawgService
    {
        private static readonly HttpClient Http = new();
        private const string BaseUrl = "https://api.rawg.io/api";

        /// <summary> Local folder where downloaded cover images are stored. </summary>
        public static readonly string CoversFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "covers");

        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

        /// <summary> Returns true if a RAWG API key is configured in settings. </summary>
        public static bool IsAvailable => !string.IsNullOrWhiteSpace(SettingsService.Current.RawgApiKey);

        /// <summary>
        /// Searches RAWG for games matching the given query.
        /// Returns an empty list if offline, key is missing, or the request fails.
        /// </summary>
        public static async Task<List<RawgGameResult>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(query)) return new();

            try
            {
                var url = $"{BaseUrl}/games?key={SettingsService.Current.RawgApiKey}" +
                          $"&search={Uri.EscapeDataString(query)}&page_size=10";
                var json = await Http.GetStringAsync(url, ct);
                var response = JsonSerializer.Deserialize<RawgSearchResponse>(json, Options);
                return response?.Results ?? new();
            }
            catch
            {
                return new();
            }
        }

        /// <summary>
        /// Fetches full game details from RAWG by id, including description and developers.
        /// Returns null if unavailable or the request fails.
        /// </summary>
        public static async Task<RawgGameDetail?> GetDetailAsync(int rawgId, CancellationToken ct = default)
        {
            if (!IsAvailable) return null;

            try
            {
                var url = $"{BaseUrl}/games/{rawgId}?key={SettingsService.Current.RawgApiKey}";
                var json = await Http.GetStringAsync(url, ct);
                return JsonSerializer.Deserialize<RawgGameDetail>(json, Options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a cover image from the given URL and saves it locally.
        /// Returns the local file path, or an empty string on failure.
        /// </summary>
        public static async Task<string> DownloadCoverAsync(string imageUrl, int rawgId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

            try
            {
                Directory.CreateDirectory(CoversFolder);
                var path = Path.Combine(CoversFolder, $"{rawgId}.jpg");

                if (File.Exists(path)) return path;

                var bytes = await Http.GetByteArrayAsync(imageUrl, ct);
                await File.WriteAllBytesAsync(path, bytes, ct);
                return path;
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary> Validates the given API key by making a test request to RAWG. </summary>
        public static async Task<bool> ValidateKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            try
            {
                var url = $"{BaseUrl}/games?key={key}&page_size=1";
                var response = await Http.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}