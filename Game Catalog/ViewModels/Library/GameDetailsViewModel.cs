using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
    public partial class GameDetailsViewModel : ViewModelBase, IDisposable
    {
        /// <summary> The game being displayed. </summary>
        public Game Game { get; private set; }

        /// <summary> Raised when the user leaves the page: back, or after archiving, restoring or deleting. </summary>
        public event Action? BackRequested;

        /// <summary> Index of the selected page tab: 0 details, 1 sessions, 2 goals, 3 DLC. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailsTab), nameof(IsSessionsTab), nameof(IsGoalsTab), nameof(IsDlcTab))]
        private int _selectedTabIndex;

        /// <summary> True when the details tab is selected. </summary>
        public bool IsDetailsTab => SelectedTabIndex == 0;

        /// <summary> True when the sessions tab is selected. </summary>
        public bool IsSessionsTab => SelectedTabIndex == 1;

        /// <summary> True when the goals tab is selected. </summary>
        public bool IsGoalsTab => SelectedTabIndex == 2;

        /// <summary> True when the DLC tab is selected. </summary>
        public bool IsDlcTab => SelectedTabIndex == 3;

        /// <summary> Sessions tab of the game. Loaded every time the tab is opened. </summary>
        public GameSessionsTabViewModel Sessions { get; }

        /// <summary> Goals tab of the game. Loaded every time the tab is opened. </summary>
        public GameGoalsTabViewModel Goals { get; }

        /// <summary> DLC tab of the game. Loaded every time the tab is opened. </summary>
        public GameDlcTabViewModel Dlc { get; }

        public string Title => Game.GameTitle;
        public bool IsArchived => Game.IsArchived;
        public GameStatus Status => Game.GameStatus;
        public string? RatingText => Game.PersonalRating is { } r ? $"{r}/10" : null;
        public string DeveloperName => Game.DeveloperName;
        public string? YearText => Game.ReleaseYear?.ToString();
        public string? SizeText => Game.SizeGB is { } s ? $"{s:0.0} ГБ" : null;
        public double TotalHours => Game.TotalHours;
        public string DaysInLibraryText => $"{Game.DaysInLibrary} дн.";
        public DateTime AddedAt => Game.AddedAt;

        public string? GenresText => Game.Genres.Count == 0
            ? null
            : string.Join(", ", Game.Genres.Select(g => g.GenreName));

        public System.Collections.Generic.IReadOnlyList<Tag> Tags => Game.Tags;
        public System.Collections.Generic.IReadOnlyList<Platform> Platforms => Game.Platforms;
        public bool HasTags => Game.Tags.Count > 0;
        public bool HasPlatforms => Game.Platforms.Count > 0;

        public string Description => Game.GameDescription;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Game.GameDescription);

        public Bitmap? CoverImage => ImageCache.Get(Game.CoverImagePath, 640);
        public bool HasCover => MediaStore.Exists(Game.CoverImagePath);

        public Bitmap? BackgroundImage => ImageCache.Get(Game.BackgroundImagePath, 1920);

        /// <summary> Small copy of the background used under the blur effect. </summary>
        public Bitmap? BlurImage => ImageCache.Get(Game.BackgroundImagePath, 400);
        public bool HasBackground => MediaStore.Exists(Game.BackgroundImagePath);

        public Bitmap? IconImage => ImageCache.Get(Game.IconPath, 128);
        public bool HasIcon => MediaStore.Exists(Game.IconPath);

        /// <summary> Session in progress for this game, or null. Loaded when the page is shown and after every change. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPlaying), nameof(ShowPlay), nameof(ShowStartSession), nameof(ElapsedText))]
        private PlaySession? _activeSession;

        /// <summary> Updates the elapsed time of the running session once a second. Runs only while a session is open. </summary>
        private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

        /// <summary> The main button is shown for active standalone games: archived games are read-only, DLC has no sessions. </summary>
        public bool HasMainAction => !Game.IsArchived && Game.ParentGameId == null;

        /// <summary> True while a session of this game is in progress. </summary>
        public bool IsPlaying => ActiveSession != null;

        /// <summary> "Play" is offered when the game has a launch file. </summary>
        public bool ShowPlay => HasMainAction && !IsPlaying && Game.ExecutablePath.Length > 0;

        /// <summary> Without a launch file the same place offers to start a session by hand. </summary>
        public bool ShowStartSession => HasMainAction && !IsPlaying && Game.ExecutablePath.Length == 0;

        /// <summary> Elapsed time of the running session as h:mm:ss, or empty if nothing is running. </summary>
        public string ElapsedText
        {
            get
            {
                if (ActiveSession is not { } session) return string.Empty;

                var elapsed = DateTime.Now - session.StartTime;
                if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
                return $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
        }

        /// <summary> True if the install folder is set, so it can be opened. </summary>
        public bool HasInstallFolder => Game.InstallPath.Length > 0;

        public GameDetailsViewModel(Game game)
        {
            Game = game;
            Goals = new GameGoalsTabViewModel(game);
            Sessions = new GameSessionsTabViewModel(game);
            Dlc = new GameDlcTabViewModel(game);

            // The total play time of the game depends on its sessions.
            Sessions.SessionsChanged += () => _ = ReloadAsync();

            _ticker.Tick += (_, _) => OnPropertyChanged(nameof(ElapsedText));
        }

        /// <summary> Loads the running session when the page is shown. </summary>
        public override void OnNavigatedTo() => _ = LoadActiveSessionAsync();

        /// <summary> Stops the timer when the page is left. </summary>
        public void Dispose() => _ticker.Stop();

        partial void OnActiveSessionChanged(PlaySession? value)
        {
            if (value != null)
                _ticker.Start();
            else
                _ticker.Stop();
        }

        /// <summary> Reads the open session of the game from the database. </summary>
        private async Task LoadActiveSessionAsync()
        {
            try
            {
                ActiveSession = SessionRepository.GetActive(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка завантаження поточної сесії", ex);
            }
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (value == 1)
                Sessions.Reload();
            else if (value == 2)
                Goals.Reload();
            else if (value == 3)
                Dlc.Reload();
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
            Sessions.TotalHours = fresh.TotalHours;
            RefreshAll();
            await LoadActiveSessionAsync();
        }

        /// <summary>
        /// Launches the game and starts a session; the session is the fallback timer until automatic tracking exists.
        /// A session is created only if the game has actually started.
        /// </summary>
        [RelayCommand]
        private async Task PlayAsync()
        {
            var path = Game.ExecutablePath;
            var outcome = GameInstall.Launch(path);

            if (outcome.Result == LaunchResult.NotExecutable && await InstallPrompts.AllowExecutionAsync(path))
                outcome = GameInstall.Launch(path);

            switch (outcome.Result)
            {
                case LaunchResult.Started:
                    // The game is already running and cannot be taken back, so a failure here is only reported.
                    await BeginSessionAsync("Гру запущено, але сесію не розпочато");
                    break;

                case LaunchResult.NotFound:
                    if (await Dialogs.Current.ConfirmAsync("Файл запуску не знайдено",
                            $"Файлу немає на цьому комп'ютері:\n{path}\n\nВідкрити редактор, щоб вказати його заново?",
                            "Відкрити редактор"))
                        await EditAsync();
                    break;

                case LaunchResult.WrongPlatform:
                    if (await Dialogs.Current.ConfirmAsync("Гра не запуститься на цій системі",
                            "Файл запуску є програмою Windows. Почати сесію без запуску гри?",
                            "Почати сесію"))
                        await BeginSessionAsync("Не вдалося розпочати сесію");
                    break;

                case LaunchResult.Failed:
                    await Dialogs.Current.ErrorAsync("Не вдалося запустити гру", path, outcome.Error!);
                    break;

                case LaunchResult.Cancelled:      // the player declined the administrator prompt

                case LaunchResult.NotExecutable:  // the player declined the execute permission
                    break;
            }
        }

        /// <summary> Starts a session by hand for a game that is not launched from the application. </summary>
        [RelayCommand]
        private Task StartSessionAsync() => BeginSessionAsync("Не вдалося розпочати сесію");

        /// <summary> Ends the running session now. </summary>
        [RelayCommand]
        private async Task StopSessionAsync()
        {
            if (ActiveSession is not { } session) return;

            try
            {
                SessionRepository.Stop(session.PlaySessionId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Не вдалося завершити сесію", ex);
                return;
            }

            await AfterSessionChangeAsync();
        }

        /// <summary> Opens the install folder in the file manager. </summary>
        [RelayCommand]
        private Task OpenInstallFolderAsync() => Dialogs.Current.OpenExistingFolderAsync(Game.InstallPath);

        /// <summary> Opens a session at the current time; the time is taken by the database. </summary>
        private async Task BeginSessionAsync(string errorTitle)
        {
            try
            {
                SessionRepository.Start(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(errorTitle, ex);
                return;
            }

            await AfterSessionChangeAsync();
        }

        /// <summary> Reloads the game (total time, running session) and the sessions tab if it is open. </summary>
        private async Task AfterSessionChangeAsync()
        {
            await ReloadAsync();
            if (IsSessionsTab)
                Sessions.Reload();
        }

        /// <summary> Opens the editor (it saves the game itself) and reloads the page. </summary>
        [RelayCommand]
        private async Task EditAsync()
        {
            var edited = await Dialogs.Current.ShowGameEditorAsync(Game);
            if (edited == null) return;

            await ReloadAsync();
        }

        /// <summary>
        /// Moves the game to the archive after confirmation.
        /// A session in progress blocks archiving, so the player is offered to end it in the same step.
        /// </summary>
        [RelayCommand]
        private async Task ArchiveAsync()
        {
            PlaySession? active;
            try
            {
                active = SessionRepository.GetActive(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка архівації гри", ex);
                return;
            }

            if (active != null)
            {
                await ArchiveEndingSessionAsync(active);
                return;
            }

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

        private async Task ArchiveEndingSessionAsync(PlaySession active)
        {
            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Архівування гри",
                $"У «{Game.GameTitle}» триває сесія (з {active.StartTime:dd.MM.yyyy HH:mm}).\n" +
                "Завершити її зараз і перенести гру в архів?\n" +
                "Гра зникне з бібліотеки та статистики, але її можна відновити в розділі «Архів».",
                "Завершити й архівувати");
            if (!confirmed) return;

            try
            {
                GameRepository.ArchiveEndingSession(Game.GameId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка архівації гри", ex);
                return;
            }

            BackRequested?.Invoke();
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