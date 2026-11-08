using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the game details page. </summary>
    public partial class GameDetailsViewModel : ViewModelBase
    {
        private const string NoValue = "—";

        /// <summary> The game being displayed. </summary>
        public Game Game { get; private set; }

        /// <summary> Raised when the user leaves the page: back, or after archiving, restoring or deleting. </summary>
        public event Action? BackRequested;

        /// <summary> Index of the selected page tab. Only the details tab exists for now. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailsTab))]
        private int _selectedTabIndex;

        /// <summary> True when the details tab is selected. </summary>
        public bool IsDetailsTab => SelectedTabIndex == 0;

        public string Title => Game.GameTitle;
        public bool IsArchived => Game.IsArchived;
        public GameStatus Status => Game.GameStatus;
        public string RatingText => Game.PersonalRating is { } r ? $"{r}/10" : NoValue;
        public string DeveloperName => string.IsNullOrWhiteSpace(Game.DeveloperName) ? "Невідомо" : Game.DeveloperName;
        public string YearText => Game.ReleaseYear?.ToString() ?? NoValue;
        public string SizeText => Game.SizeGB is { } s ? $"{s:0.0} ГБ" : "Невідомо";
        public double TotalHours => Game.TotalHours;
        public string DaysInLibraryText => $"{Game.DaysInLibrary} дн.";
        public DateTime AddedAt => Game.AddedAt;

        public string GenresText => Game.Genres.Count == 0
            ? NoValue
            : string.Join(", ", Game.Genres.Select(g => g.GenreName));

        public System.Collections.Generic.IReadOnlyList<Tag> Tags => Game.Tags;
        public System.Collections.Generic.IReadOnlyList<Platform> Platforms => Game.Platforms;
        public bool HasTags => Game.Tags.Count > 0;
        public bool HasPlatforms => Game.Platforms.Count > 0;

        public string Description => Game.GameDescription;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Game.GameDescription);

        public Bitmap? CoverImage => ImageCache.Get(Game.CoverImagePath, 640);
        public bool HasCover => File.Exists(Game.CoverImagePath);

        public Bitmap? BackgroundImage => ImageCache.Get(Game.BackgroundImagePath, 1920);

        /// <summary> Small copy of the background used under the blur effect. </summary>
        public Bitmap? BlurImage => ImageCache.Get(Game.BackgroundImagePath, 400);
        public bool HasBackground => File.Exists(Game.BackgroundImagePath);

        public Bitmap? IconImage => ImageCache.Get(Game.IconPath, 128);
        public bool HasIcon => File.Exists(Game.IconPath);

        public GameDetailsViewModel(Game game)
        {
            Game = game;
        }

        /// <summary> Raises change notifications for everything shown on the page. </summary>
        private void RefreshAll()
        {
            OnPropertyChanged(string.Empty);
        }

        /// <summary> Reloads the game from the database. Goes back if it no longer exists. </summary>
        private async Task ReloadAsync()
        {
            Game? fresh;
            try
            {
                fresh = GameRepository.GetById(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка завантаження гри", ex);
                return;
            }

            if (fresh == null)
            {
                BackRequested?.Invoke();
                return;
            }

            Game = fresh;
            RefreshAll();
        }

        /// <summary> Opens the editor (it saves the game itself) and reloads the page. </summary>
        [RelayCommand]
        private async Task EditAsync()
        {
            var edited = await Dialogs.Current.ShowGameEditorAsync(Game);
            if (edited == null) return;

            await ReloadAsync();
        }

        /// <summary> Moves the game to the archive after confirmation. </summary>
        [RelayCommand]
        private async Task ArchiveAsync()
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Архівування гри",
                $"Перенести «{Game.GameTitle}» в архів?\n" +
                "Гра зникне з бібліотеки та статистики, але її можна відновити в розділі «Архів».",
                "В архів");
            if (!confirmed) return;

            await SetArchivedAsync(true);
        }

        /// <summary> Returns the game from the archive to the library after confirmation. </summary>
        [RelayCommand]
        private async Task RestoreAsync()
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Відновлення гри",
                $"Повернути «{Game.GameTitle}» в бібліотеку?\n" +
                "Гра знову з'явиться в бібліотеці та статистиці.",
                "Відновити");
            if (!confirmed) return;

            await SetArchivedAsync(false);
        }

        private async Task SetArchivedAsync(bool archived)
        {
            try
            {
                GameRepository.SetArchived(Game.GameId, archived);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка архівації гри", ex);
                return;
            }

            BackRequested?.Invoke();
        }

        /// <summary> Deletes the game after confirmation. Sessions, goals, achievements and DLC are removed by the database. </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення гри",
                $"Видалити «{Game.GameTitle}»?\n" +
                "Разом із грою буде видалено її сесії, цілі, досягнення та доповнення. " +
                "Цю дію не можна скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                GameRepository.Delete(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення гри", ex);
                return;
            }

            BackRequested?.Invoke();
        }

        /// <summary> Navigates back to the previous page. </summary>
        [RelayCommand]
        private void GoBack() => BackRequested?.Invoke();

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}