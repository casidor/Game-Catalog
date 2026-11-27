using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Media storage of the application. The database keeps paths relative to <see cref="AppPaths.Root"/>
    /// with forward slashes (for example "media/games/5/covers/&lt;guid&gt;.png"), so it works on any OS.
    /// Files are copied here only when the owning record is saved; unused files are removed by the user, never automatically.
    /// </summary>
    public static class MediaStore
    {
        /// <summary> File of the media folder: stored path and size in bytes. </summary>
        public sealed record MediaFile(string Path, long Bytes);

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

        /// <summary> Absolute folder for the icon of the platform. </summary>
        public static string PlatformFolder(int platformId) =>
            Path.Combine(Folder, "platforms", platformId.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Puts the image into the game folder and returns its stored path.
        /// Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> on failure.
        /// </summary>
        public static string ImportGameImage(string source, int gameId, ImageKind kind) =>
            Import(source, GameFolder(gameId, kind));

        /// <summary>
        /// Puts the icon into the platform folder and returns its stored path.
        /// Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> on failure.
        /// </summary>
        public static string ImportPlatformIcon(string source, int platformId) =>
            Import(source, PlatformFolder(platformId));

        /// <summary>
        /// Copies the file into the folder under a new unique name and returns its stored path.
        /// A file already in that folder is reused without copying; a downloaded temporary file is removed after copying.
        /// </summary>
        private static string Import(string source, string folder)
        {
            var fullSource = Path.GetFullPath(source);

            if (string.Equals(Path.GetDirectoryName(fullSource), Path.GetFullPath(folder), PathComparison))
                return ToStored(fullSource);

            Directory.CreateDirectory(folder);
            var dest = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(fullSource).ToLowerInvariant()}");
            File.Copy(fullSource, dest);

            if (string.Equals(Path.GetDirectoryName(fullSource), TempFolder, PathComparison))
                TryDelete(fullSource);

            return ToStored(dest);
        }

        /// <summary> Returns all files of the media folder with their sizes. Throws <see cref="IOException"/> if the folder cannot be read. </summary>
        public static List<MediaFile> GetFiles()
        {
            if (!Directory.Exists(Folder)) return new List<MediaFile>();

            return Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)
                .Select(file => new MediaFile(ToStored(file), new FileInfo(file).Length))
                .ToList();
        }

        /// <summary> Returns the files whose paths are not among the referenced ones. Letter case is ignored on Windows only. </summary>
        public static List<MediaFile> FindUnused(IEnumerable<MediaFile> files, IEnumerable<string> referencedPaths)
        {
            var referenced = new HashSet<string>(referencedPaths,
                OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            return files.Where(f => !referenced.Contains(f.Path)).ToList();
        }

        /// <summary>
        /// Deletes the files, then the folders left empty inside the media folder.
        /// Paths outside the media folder are skipped. Returns the number of files that could not be deleted.
        /// </summary>
        public static int DeleteFiles(IEnumerable<string> paths)
        {
            var root = Path.GetFullPath(Folder) + Path.DirectorySeparatorChar;
            var failed = 0;

            foreach (var path in paths)
            {
                var full = Path.GetFullPath(Resolve(path));
                if (!full.StartsWith(root, PathComparison))
                {
                    failed++;
                    continue;
                }

                try { File.Delete(full); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed++; }
            }

            RemoveEmptyFolders();
            return failed;
        }

        /// <summary> Removes empty folders inside the media folder, deepest first. The media folder itself stays. </summary>
        private static void RemoveEmptyFolders()
        {
            if (!Directory.Exists(Folder)) return;

            var folders = Directory.EnumerateDirectories(Folder, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();

            foreach (var folder in folders)
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(folder).Any())
                        Directory.Delete(folder);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
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
