using System;
using System.Globalization;
using System.IO;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Media storage of the application. The database keeps paths relative to <see cref="AppPaths.Root"/>
    /// with forward slashes (for example "media/games/5/covers/&lt;guid&gt;.png"), so it works on any OS.
    /// Files are copied here only when the owning record is saved; unused files are removed by the user, never automatically.
    /// </summary>
    public static class MediaStore
    {
        /// <summary> Absolute path of the media folder. </summary>
        public static readonly string Folder = Path.Combine(AppPaths.Root, "media");

        /// <summary> Folder for downloaded images that are not saved yet. Lives outside the media folder. </summary>
        private static readonly string TempFolder = Path.Combine(Path.GetTempPath(), "GameCatalog");

        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        /// <summary> True if the path is a stored relative media path, false for an empty or absolute (not yet imported) path. </summary>
        public static bool IsStored(string? path) =>
            !string.IsNullOrEmpty(path) && !Path.IsPathRooted(path);

        /// <summary> Returns the absolute path for a stored path; absolute paths are returned unchanged. </summary>
        public static string Resolve(string? path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return IsStored(path) ? Path.Combine(AppPaths.Root, path) : path;
        }

        /// <summary> True if the file behind the path exists. </summary>
        public static bool Exists(string? path) =>
            !string.IsNullOrEmpty(path) && File.Exists(Resolve(path));

        /// <summary> Absolute folder for images of the given kind of the game. </summary>
        public static string GameFolder(int gameId, ImageKind kind) =>
            Path.Combine(Folder, "games", gameId.ToString(CultureInfo.InvariantCulture), kind switch
            {
                ImageKind.Cover => "covers",
                ImageKind.Background => "backgrounds",
                _ => "icons"
            });

        /// <summary>
        /// Puts the image into the game folder and returns its stored path.
        /// A file already in that folder is reused without copying; a downloaded temporary file is removed after copying.
        /// Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> on failure.
        /// </summary>
        public static string ImportGameImage(string source, int gameId, ImageKind kind)
        {
            var fullSource = Path.GetFullPath(source);
            var folder = GameFolder(gameId, kind);

            if (string.Equals(Path.GetDirectoryName(fullSource), Path.GetFullPath(folder), PathComparison))
                return ToStored(fullSource);

            Directory.CreateDirectory(folder);
            var dest = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(fullSource).ToLowerInvariant()}");
            File.Copy(fullSource, dest);

            if (string.Equals(Path.GetDirectoryName(fullSource), TempFolder, PathComparison))
                TryDelete(fullSource);

            return ToStored(dest);
        }

        /// <summary> Returns a unique path in the temporary folder for a downloaded image, creating the folder if needed. </summary>
        /// <param name="extension">File extension including the dot, for example ".png".</param>
        public static string NewTempFilePath(string extension)
        {
            Directory.CreateDirectory(TempFolder);
            return Path.Combine(TempFolder, $"{Guid.NewGuid()}{extension}");
        }

        /// <summary> Removes downloads left from cancelled editors. Called at start; failures are ignored. </summary>
        public static void ClearTemp()
        {
            try
            {
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        /// <summary> Converts an absolute path inside the root to the stored form with forward slashes. </summary>
        private static string ToStored(string fullPath) =>
            Path.GetRelativePath(AppPaths.Root, fullPath).Replace('\\', '/');

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
