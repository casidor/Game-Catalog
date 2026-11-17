using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game_Catalog.Services
{
    /// <summary>
    /// In-memory LRU cache of bitmaps decoded to a given width.
    /// Must be used from the UI thread only.
    /// </summary>
    public static class ImageCache
    {
        private const long BudgetBytes = 192L * 1024 * 1024;

        private sealed record Entry(string Path, int Width, Bitmap Bitmap, long Bytes);

        private static readonly Dictionary<(string Path, int Width), LinkedListNode<Entry>> Map = new();

        /// <summary> First node = most recently used. </summary>
        private static readonly LinkedList<Entry> Order = new();

        private static long _totalBytes;

        /// <summary> Returns the image decoded to the given width, or null if it cannot be loaded. </summary>
        public static Bitmap? Get(string? path, int width)
        {
            if (string.IsNullOrEmpty(path) || width <= 0 || !File.Exists(path))
                return null;

            if (Map.TryGetValue((path, width), out var node))
            {
                Order.Remove(node);
                Order.AddFirst(node);
                return node.Value.Bitmap;
            }

            Bitmap bitmap;
            try
            {
                using var stream = File.OpenRead(path);
                bitmap = Bitmap.DecodeToWidth(stream, width, BitmapInterpolationMode.HighQuality);
            }
            catch
            {
                return null;
            }

            long bytes = (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4;
            var entry = new Entry(path, width, bitmap, bytes);
            Map[(path, width)] = Order.AddFirst(entry);
            _totalBytes += bytes;

            // Evict least recently used entries, but never the one just added.
            while (_totalBytes > BudgetBytes && Order.Count > 1)
                Remove(Order.Last!);

            return bitmap;
        }

        /// <summary> Drops all cached sizes of the given file. </summary>
        public static void Invalidate(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            foreach (var node in Order.Where(e => e.Path == path).ToList()
                         .Select(e => Map[(e.Path, e.Width)]))
                Remove(node);
        }

        private static void Remove(LinkedListNode<Entry> node)
        {
            Order.Remove(node);
            Map.Remove((node.Value.Path, node.Value.Width));
            _totalBytes -= node.Value.Bytes;
        }
    }
}