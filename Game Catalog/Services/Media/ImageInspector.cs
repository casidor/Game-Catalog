using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace Game_Catalog.Services
{
    /// <summary> Kind of a game image; defines recommended size and aspect ratio. </summary>
    public enum ImageKind { Cover, Background, Icon }

    /// <summary> Info line and warnings about a selected image. </summary>
    public sealed class ImageReport
    {
        public static readonly ImageReport Empty = new(string.Empty, string.Empty);

        public string Info { get; }
        public string Warning { get; }
        public bool HasWarning => Warning.Length > 0;

        public ImageReport(string info, string warning)
        {
            Info = info;
            Warning = warning;
        }
    }

    /// <summary> Reads image dimensions and produces non-blocking quality warnings. </summary>
    public static class ImageInspector
    {
        private const long MaxBytes = 10L * 1024 * 1024;
        private const double RatioTolerance = 0.15;

        /// <summary> Returns true if the file can be decoded as an image. </summary>
        public static bool IsReadable(string path)
        {
            try
            {
                using var bmp = new Bitmap(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Builds the info line and warnings for the given file. </summary>
        public static ImageReport Inspect(string? path, ImageKind kind)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return ImageReport.Empty;

            long bytes;
            int w, h;
            try
            {
                bytes = new FileInfo(path).Length;
                using var bmp = new Bitmap(path);
                w = bmp.PixelSize.Width;
                h = bmp.PixelSize.Height;
            }
            catch
            {
                return new ImageReport(string.Empty, "Файл не вдалося прочитати.");
            }

            var (minW, minH, ratio, ratioLabel) = kind switch
            {
                ImageKind.Cover => (300, 400, 3.0 / 4.0, "3:4"),
                ImageKind.Background => (1280, 720, 16.0 / 9.0, "16:9"),
                _ => (64, 64, 1.0, "1:1")
            };

            var warnings = new System.Collections.Generic.List<string>();

            if (w < minW || h < minH)
                warnings.Add($"Замале зображення, рекомендовано від {minW}×{minH}.");

            var actual = (double)w / h;
            if (Math.Abs(actual - ratio) / ratio > RatioTolerance)
                warnings.Add($"Співвідношення сторін далеке від {ratioLabel}, краї буде обрізано.");

            if (bytes > MaxBytes)
                warnings.Add($"Файл завеликий ({bytes / 1048576.0:0.0} МБ), рекомендовано до 10 МБ.");

            return new ImageReport(
                $"{w}×{h} · {bytes / 1048576.0:0.0} МБ",
                string.Join("\n", warnings));
        }
    }
}