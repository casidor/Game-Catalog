using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Game_Catalog.Services
{
    /// <summary> How a launch attempt ended. </summary>
    public enum LaunchResult
    {
        /// <summary> The process was started. </summary>
        Started,
        /// <summary> The launch file is not set or does not exist. </summary>
        NotFound,
        /// <summary> Linux: the file has no execute permission. </summary>
        NotExecutable,
        /// <summary> The file is a Windows program and cannot run on this OS by itself. </summary>
        WrongPlatform,
        /// <summary> Windows: the user declined the administrator prompt. </summary>
        Cancelled,
        /// <summary> The OS refused to start the file; see the error. </summary>
        Failed
    }

    /// <summary> Result of a launch attempt with the error for <see cref="LaunchResult.Failed"/>. </summary>
    public sealed record LaunchOutcome(LaunchResult Result, Exception? Error = null);

    /// <summary>
    /// Installed games on this machine: path normalization, size measuring and launching.
    /// Paths are absolute and valid only on this machine; the database just stores them.
    /// </summary>
    public static class GameInstall
    {
        private const double BytesInGb = 1024d * 1024 * 1024;

        /// <summary> Win32 ERROR_CANCELLED: the user declined the administrator (UAC) prompt. </summary>
        private const int ErrorCancelled = 1223;

        /// <summary> Extensions of Windows programs and shortcuts that other OSes cannot start by themselves. </summary>
        private static readonly string[] WindowsOnlyExtensions = [".exe", ".bat", ".cmd", ".lnk", ".url", ".msi"];

        /// <summary>
        /// Returns the full path without a trailing separator, so "D:\Games\X\" and "D:\Games\X" are stored the same.
        /// Empty input gives an empty string; an invalid path is returned trimmed and left to the existence checks.
        /// </summary>
        public static string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            try
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return path.Trim();
            }
        }

        /// <summary> True if the install folder is set and exists. </summary>
        public static bool FolderExists(string? installPath) =>
            !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath);

        /// <summary> True if the launch file is set and exists. </summary>
        public static bool LaunchFileExists(string? executablePath) =>
            !string.IsNullOrEmpty(executablePath) && File.Exists(executablePath);

        /// <summary>
        /// Measures the size in GB: of the folder if it is set, otherwise of the launch file (portable games).
        /// Returns null if neither exists. Inaccessible files and symbolic links are skipped.
        /// </summary>
        public static Task<double?> MeasureAsync(string? installPath, string? executablePath, CancellationToken token) =>
            Task.Run(() =>
            {
                long? bytes = null;
                if (!string.IsNullOrEmpty(installPath))
                {
                    if (Directory.Exists(installPath))
                        bytes = FolderBytes(installPath, token);
                }
                else if (LaunchFileExists(executablePath))
                {
                    bytes = new FileInfo(executablePath!).Length;
                }

                return bytes is { } b ? Math.Round(b / BytesInGb, 2) : (double?)null;
            }, token);

        /// <summary> True if the file is a Windows program and this OS is not Windows. </summary>
        public static bool IsWrongPlatform(string executablePath) =>
            !OperatingSystem.IsWindows()
            && Array.IndexOf(WindowsOnlyExtensions, Path.GetExtension(executablePath).ToLowerInvariant()) >= 0;

        /// <summary> Linux: true if the user may not execute the file. Always false on Windows. </summary>
        public static bool NeedsExecutePermission(string executablePath)
        {
            if (OperatingSystem.IsWindows()) return false;
            return (File.GetUnixFileMode(executablePath) & UnixFileMode.UserExecute) == 0;
        }

        /// <summary> Linux: allows the owner to execute the file. Does nothing on Windows. Throws on IO errors. </summary>
        public static void MakeExecutable(string executablePath)
        {
            if (OperatingSystem.IsWindows()) return;
            File.SetUnixFileMode(executablePath, File.GetUnixFileMode(executablePath) | UnixFileMode.UserExecute);
        }

        /// <summary>
        /// Starts the launch file with its folder as the working directory (many games load files relative to it).
        /// Windows: through the shell, like a double click in Explorer, so .exe, .bat, .lnk and .url all work.
        /// Linux: directly, because the shell (xdg-open) would open the file in an editor instead of running it.
        /// </summary>
        public static LaunchOutcome Launch(string? executablePath)
        {
            if (!LaunchFileExists(executablePath)) return new(LaunchResult.NotFound);

            var path = executablePath!;
            if (IsWrongPlatform(path)) return new(LaunchResult.WrongPlatform);
            if (NeedsExecutePermission(path)) return new(LaunchResult.NotExecutable);

            var info = new ProcessStartInfo(path)
            {
                UseShellExecute = OperatingSystem.IsWindows(),
                WorkingDirectory = Path.GetDirectoryName(path) ?? string.Empty
            };

            try
            {
                // Disposing only releases the handle; the game keeps running.
                using var process = Process.Start(info);
                return new(LaunchResult.Started);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
            {
                return new(LaunchResult.Cancelled);
            }
            catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
            {
                return new(LaunchResult.Failed, ex);
            }
        }

        /// <summary> Sums the sizes of all files under the folder, checking for cancellation per file. </summary>
        private static long FolderBytes(string folder, CancellationToken token)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                // Hidden and system files are part of the game; links may point outside it or loop.
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            long total = 0;
            foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*", options))
            {
                token.ThrowIfCancellationRequested();
                try { total += file.Length; }
                catch (FileNotFoundException) { } // removed while measuring
            }
            return total;
        }
    }
}
