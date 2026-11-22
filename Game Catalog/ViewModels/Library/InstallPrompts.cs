using Game_Catalog.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> Dialogs about game installation shared by the game editor and the details page. </summary>
    internal static class InstallPrompts
    {
        /// <summary> Linux: offers to allow running a file that has no execute permission. Returns true if it was allowed. </summary>
        public static async Task<bool> AllowExecutionAsync(string path)
        {
            var allow = await Dialogs.Current.ConfirmAsync("Файл не виконуваний",
                "Система не дозволяє запускати цей файл. Дозволити запуск?\n\n" +
                "Зміниться лише дозвіл файлу, його вміст залишиться тим самим.",
                "Дозволити");
            if (!allow) return false;

            try
            {
                GameInstall.MakeExecutable(path);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Dialogs.Current.ErrorAsync("Не вдалося змінити дозвіл", path, ex);
                return false;
            }
        }
    }
}
