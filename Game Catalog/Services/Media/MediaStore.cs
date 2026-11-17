using System;
using System.IO;

namespace Game_Catalog.Services
{
    /// <summary> Single place that decides where imported and downloaded images are stored. </summary>
    public static class MediaStore
    {
        /// <summary> Folder where all imported and downloaded images are stored. </summary>
        public static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "media");

        /// <summary> Returns a unique file path inside the folder, creating the folder if needed. </summary>
        /// <param name="extension">File extension including the dot, for example ".png".</param>
        public static string NewFilePath(string extension)
        {
            Directory.CreateDirectory(Folder);
            return Path.Combine(Folder, $"{Guid.NewGuid()}{extension}");
        }
    }
}
