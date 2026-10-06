using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Game_Catalog.Services
{
    /// <summary> Downloads an image by URL into the local covers folder. </summary>
    public static class ImageDownloader
    {
        private const long MaxBytes = 20L * 1024 * 1024;
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        /// <summary>
        /// Downloads the image and returns the local file path.
        /// Throws <see cref="InvalidOperationException"/> with a user-friendly message on failure.
        /// </summary>
        public static async Task<string> DownloadAsync(string url, CancellationToken ct = default)
        {
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("Вкажіть коректну адресу, що починається з http:// або https://");

            try
            {
                using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength > MaxBytes)
                    throw new InvalidOperationException("Файл завеликий (понад 20 МБ).");

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var ms = new MemoryStream();
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    ms.Write(buffer, 0, read);
                    if (ms.Length > MaxBytes)
                        throw new InvalidOperationException("Файл завеликий (понад 20 МБ).");
                }

                var bytes = ms.ToArray();
                var ext = DetectExtension(bytes)
                    ?? throw new InvalidOperationException("За цією адресою не знайдено зображення JPG, PNG або WEBP.");

                Directory.CreateDirectory(RawgService.CoversFolder);
                var dest = Path.Combine(RawgService.CoversFolder, $"{Guid.NewGuid()}{ext}");
                await File.WriteAllBytesAsync(dest, bytes, ct);

                if (!ImageInspector.IsReadable(dest))
                {
                    File.Delete(dest);
                    throw new InvalidOperationException("Не вдалося прочитати завантажене зображення.");
                }
                return dest;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Не вдалося завантажити файл: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new InvalidOperationException("Час очікування відповіді вичерпано.");
            }
        }

        private static string? DetectExtension(byte[] b)
        {
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                              && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }
    }
}