using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit game dialog. The dialog saves the game itself, so a failed save keeps the form open. </summary>
    public partial class AddGameViewModel : ViewModelBase
    {
        private const string NotSetText = "Не вказано";

        /// <summary> Title of the game. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(200, ErrorMessage = "Назва не може перевищувати 200 символів")]
        [NameText]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary> Developer studio of the game. </summary>
        [Required(ErrorMessage = "Оберіть студію-розробника")]
        [NotifyDataErrorInfo]
        [NotifyPropertyChangedFor(nameof(StudioButtonText))]
        [ObservableProperty]
        private Studio? _selectedStudio;

        /// <summary> Release year. Null if unknown. </summary>
        [CurrentYearRange(YearLimits.MinReleaseYear, YearLimits.ReleaseYearsAhead)]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int? _releaseYear;

        /// <summary> Playing status. </summary>
        [ObservableProperty]
        private GameStatus _status = GameStatus.Planned;

        /// <summary> Personal rating from 1 to 10. Null if not rated. </summary>
        [Range(1, 10, ErrorMessage = "Оцінка від 1 до 10")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int? _personalRating;

        /// <summary> Description of the game. </summary>
        [MaxLength(1000, ErrorMessage = "Опис не може перевищувати 1000 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary> Cover image: a stored media path, or an absolute path of a file picked but not imported yet. </summary>
        [ObservableProperty] private string _coverImagePath = string.Empty;

        /// <summary> Background image: a stored media path, or an absolute path of a file picked but not imported yet. </summary>
        [ObservableProperty] private string _backgroundImagePath = string.Empty;

        /// <summary> Icon: a stored media path, or an absolute path of a file picked but not imported yet. </summary>
        [ObservableProperty] private string _iconPath = string.Empty;

        /// <summary> Info and warnings for the selected cover. </summary>
        [ObservableProperty] private ImageReport _coverReport = ImageReport.Empty;

        /// <summary> Info and warnings for the selected background. </summary>
        [ObservableProperty] private ImageReport _backgroundReport = ImageReport.Empty;

        /// <summary> Info and warnings for the selected icon. </summary>
        [ObservableProperty] private ImageReport _iconReport = ImageReport.Empty;

        partial void OnCoverImagePathChanged(string value) =>
            CoverReport = ImageInspector.Inspect(value, ImageKind.Cover);

        partial void OnBackgroundImagePathChanged(string value) =>
            BackgroundReport = ImageInspector.Inspect(value, ImageKind.Background);

        partial void OnIconPathChanged(string value) =>
            IconReport = ImageInspector.Inspect(value, ImageKind.Icon);

        /// <summary> Index of the selected form tab (0 = details, 1 = media, 2 = installation). </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailsTab))]
        [NotifyPropertyChangedFor(nameof(IsMediaTab))]
        [NotifyPropertyChangedFor(nameof(IsInstallTab))]
        private int _selectedTabIndex;

        /// <summary> True when the details tab is selected. </summary>
        public bool IsDetailsTab => SelectedTabIndex == 0;

        /// <summary> True when the media tab is selected. </summary>
        public bool IsMediaTab => SelectedTabIndex == 1;

        /// <summary> True when the installation tab is selected. </summary>
        public bool IsInstallTab => SelectedTabIndex == 2;

        /// <summary>
        /// True for a DLC. It has no installation of its own, takes its genres, platforms and tags from the main game,
        /// and needs only a cover, since it has no details page and is never launched.
        /// </summary>
        public bool IsDlc => _parentGameId != null;

        /// <summary> False for a DLC: it lives in the folder of its main game and has no installation of its own. </summary>
        public bool IsInstallAvailable => !IsDlc;

        /// <summary> Install folder: a normalized absolute path, or empty if not set. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFolderMissing))]
        [NotifyCanExecuteChangedFor(nameof(MeasureCommand))]
        private string _installPath = string.Empty;

        /// <summary> Launch file: a normalized absolute path, or empty if not set. It may lie outside the install folder. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLaunchFileMissing))]
        [NotifyCanExecuteChangedFor(nameof(MeasureCommand))]
        private string _executablePath = string.Empty;

        /// <summary> Size on disk in GB, or null if not measured. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SizeText))]
        private double? _sizeGb;

        /// <summary> True while the size is being measured. </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MeasureCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private bool _isMeasuring;

        /// <summary> Cancels the running measurement; null when nothing is measured. </summary>
        private CancellationTokenSource? _measuring;

        /// <summary> True if the folder is set but does not exist on this machine. </summary>
        public bool IsFolderMissing => InstallPath.Length > 0 && !GameInstall.FolderExists(InstallPath);

        /// <summary> True if the launch file is set but does not exist on this machine. </summary>
        public bool IsLaunchFileMissing => ExecutablePath.Length > 0 && !GameInstall.LaunchFileExists(ExecutablePath);

        /// <summary> Size for display, or null if not measured. </summary>
        public string? SizeText => SizeGb is { } s ? $"{s:0.0} ГБ" : null;

        /// <summary> Studios offered in the dropdown. </summary>
        public ObservableCollection<Studio> Studios { get; } = new();

        /// <summary> Text typed in the studio search box. </summary>
        [ObservableProperty] private string _studioSearchText = string.Empty;

        /// <summary> Studios matching the search text. </summary>
        public ObservableCollection<Studio> FilteredStudios { get; } = new();

        /// <summary> Caption of the studio button. </summary>
        public string StudioButtonText => SelectedStudio?.StudioName ?? "Оберіть студію";

        /// <summary> Validation message of the studio field, shown under the button. </summary>
        public string StudioError =>
            GetErrors(nameof(SelectedStudio)).FirstOrDefault()?.ErrorMessage ?? string.Empty;

        /// <summary> True when the studio field has a validation message. </summary>
        public bool HasStudioError => StudioError.Length > 0;

        partial void OnStudioSearchTextChanged(string value) => UpdateFilteredStudios();

        /// <summary> Rebuilds the studio list for the current search text. </summary>
        private void UpdateFilteredStudios()
        {
            var query = StudioSearchText.Trim();

            FilteredStudios.Clear();
            foreach (var studio in Studios)
            {
                if (query.Length == 0 || studio.StudioName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    FilteredStudios.Add(studio);
            }
        }

        /// <summary> Applies the studio picked in the list. </summary>
        public void PickStudio(Studio studio) => SelectedStudio = studio;

        /// <summary> Genres dropdown. </summary>
        public MultiSelectField Genres { get; } = new(NotSetText);

        /// <summary> Platforms dropdown. </summary>
        public MultiSelectField Platforms { get; } = new(NotSetText);

        /// <summary> Tags dropdown. </summary>
        public MultiSelectField Tags { get; } = new(NotSetText);

        /// <summary> Available statuses. </summary>
        public GameStatus[] Statuses { get; } = Enum.GetValues<GameStatus>();

        /// <summary> The game as saved by the dialog, or null if the dialog was cancelled. </summary>
        public Game? SavedGame { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly Game? _source;

        /// <summary> Main game of a DLC; null for a standalone game. </summary>
        private readonly int? _parentGameId;

        /// <summary> Initializes the dialog for adding a new game. </summary>
        public AddGameViewModel()
        {
            WindowTitle = "Додати гру";
            ErrorsChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelectedStudio))
                {
                    OnPropertyChanged(nameof(StudioError));
                    OnPropertyChanged(nameof(HasStudioError));
                }
            };
            LoadStudios();
            LoadGenres();
            LoadPlatforms();
            LoadTags();
        }

        /// <summary> Initializes the dialog for adding a DLC to the given game: the main game is fixed and its studio is preselected. </summary>
        public static AddGameViewModel ForNewDlc(Game parent) => new(parent.GameId, parent.DeveloperId);

        private AddGameViewModel(int parentGameId, int developerId) : this()
        {
            WindowTitle = "Додати доповнення";
            _parentGameId = parentGameId;
            SelectedStudio = Studios.FirstOrDefault(s => s.StudioId == developerId);
        }

        /// <summary> Initializes the dialog for editing an existing game or DLC. </summary>
        public AddGameViewModel(Game game) : this()
        {
            WindowTitle = game.ParentGameId == null ? "Редагувати гру" : "Редагувати доповнення";
            _source = game;
            _parentGameId = game.ParentGameId;

            Title = game.GameTitle;
            SelectedStudio = Studios.FirstOrDefault(s => s.StudioId == game.DeveloperId);
            ReleaseYear = game.ReleaseYear;
            Status = game.GameStatus;
            PersonalRating = game.PersonalRating;
            Description = game.GameDescription;
            CoverImagePath = game.CoverImagePath;
            BackgroundImagePath = game.BackgroundImagePath;
            IconPath = game.IconPath;
            InstallPath = game.InstallPath;
            ExecutablePath = game.ExecutablePath;
            SizeGb = game.SizeGB;

            Genres.SetChecked(game.Genres.Select(g => g.GenreId));
            Platforms.SetChecked(game.Platforms.Select(p => p.PlatformId));
            Tags.SetChecked(game.Tags.Select(t => t.TagId));
        }

        private void LoadStudios()
        {
            try
            {
                var studios = StudioRepository.GetAll(new StudioFilter());
                Studios.Clear();
                foreach (var studio in studios)
                    Studios.Add(studio);
                UpdateFilteredStudios();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження студій", ex);
            }
        }

        private void LoadGenres(int? alsoCheckId = null)
        {
            try
            {
                Genres.Refill(
                    GenreRepository.GetAll().Select(g => new FilterOption(g.GenreName, g.GenreId)), alsoCheckId);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження жанрів", ex);
            }
        }

        private void LoadPlatforms(int? alsoCheckId = null)
        {
            try
            {
                Platforms.Refill(
                    PlatformRepository.GetAll().Select(p => new FilterOption(p.PlatformName, p.PlatformId, p.PlatformIconPath)), alsoCheckId);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження платформ", ex);
            }
        }

        private void LoadTags(int? alsoCheckId = null)
        {
            try
            {
                Tags.Refill(
                    TagRepository.GetAll().Select(t => new FilterOption(t.TagName, t.TagId)), alsoCheckId);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження тегів", ex);
            }
        }

        /// <summary> Adds a new studio from the dialog and selects it. </summary>
        [RelayCommand]
        private async Task AddStudioAsync()
        {
            var studio = await Dialogs.Current.ShowStudioEditorAsync();
            if (studio == null) return;

            try
            {
                StudioRepository.Insert(studio);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження студії", ex);
                return;
            }

            LoadStudios();
            SelectedStudio = Studios.FirstOrDefault(s => s.StudioId == studio.StudioId);
        }

        /// <summary> Adds a new genre from the dialog and ticks it. </summary>
        [RelayCommand]
        private async Task AddGenreAsync()
        {
            var genre = await Dialogs.Current.ShowGenreEditorAsync();
            if (genre == null) return;

            try
            {
                GenreRepository.Insert(genre);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження жанру", ex);
                return;
            }

            LoadGenres(genre.GenreId);
        }

        /// <summary> Adds a new platform from the dialog and ticks it. </summary>
        [RelayCommand]
        private async Task AddPlatformAsync()
        {
            var platform = await Dialogs.Current.ShowPlatformEditorAsync();
            if (platform == null) return;

            try
            {
                PlatformRepository.Insert(platform);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження платформи", ex);
                return;
            }

            LoadPlatforms(platform.PlatformId);
        }

        /// <summary> Adds a new tag from the dialog and ticks it. </summary>
        [RelayCommand]
        private async Task AddTagAsync()
        {
            var tag = await Dialogs.Current.ShowTagEditorAsync();
            if (tag == null) return;

            try
            {
                TagRepository.Insert(tag);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження тегу", ex);
                return;
            }

            LoadTags(tag.TagId);
        }

        /// <summary> Lets the user pick an image file for the given kind. </summary>
        [RelayCommand]
        private async Task PickImageAsync(ImageKind kind)
        {
            var title = kind switch
            {
                ImageKind.Cover => "Оберіть обкладинку",
                ImageKind.Background => "Оберіть фон",
                _ => "Оберіть іконку"
            };

            // An existing game opens its own folder of this kind, so a previously used image can be picked again.
            var startFolder = _source != null ? MediaStore.GameFolder(_source.GameId, kind) : null;
            var path = await Dialogs.Current.PickImageAsync(title, startFolder);
            if (path != null)
                SetImage(kind, path);
        }

        /// <summary> Downloads an image by URL for the given kind. </summary>
        [RelayCommand]
        private async Task DownloadImageAsync(ImageKind kind)
        {
            var path = await Dialogs.Current.DownloadImageAsync();
            if (path != null)
                SetImage(kind, path);
        }

        /// <summary> Removes the image of the given kind. </summary>
        [RelayCommand]
        private void ClearImage(ImageKind kind) => SetImage(kind, string.Empty);

        /// <summary> Assigns the image path of the given kind and drops the cached bitmap of the old file. </summary>
        private void SetImage(ImageKind kind, string path)
        {
            switch (kind)
            {
                case ImageKind.Cover:
                    ImageCache.Invalidate(CoverImagePath);
                    CoverImagePath = path;
                    break;
                case ImageKind.Background:
                    ImageCache.Invalidate(BackgroundImagePath);
                    BackgroundImagePath = path;
                    break;
                case ImageKind.Icon:
                    ImageCache.Invalidate(IconPath);
                    IconPath = path;
                    break;
            }
        }

        /// <summary> Lets the user pick the install folder. </summary>
        [RelayCommand]
        private async Task PickInstallFolderAsync()
        {
            var picked = await Dialogs.Current.PickFolderAsync("Тека встановлення гри", InstallPath);
            if (picked != null)
                SetInstallPath(GameInstall.Normalize(picked));
        }

        /// <summary> Removes the install folder. </summary>
        [RelayCommand]
        private void ClearInstallFolder() => SetInstallPath(string.Empty);

        /// <summary>
        /// Changes the install folder. The size is reset, since it belonged to the old folder,
        /// and a launch file inside the old folder is removed, since it no longer points to this installation.
        /// </summary>
        private void SetInstallPath(string path)
        {
            if (GameInstall.SamePath(path, InstallPath)) return;

            StopMeasuring();
            if (GameInstall.IsInside(ExecutablePath, InstallPath))
                ExecutablePath = string.Empty;
            InstallPath = path;
            SizeGb = null;
        }

        /// <summary> Lets the user pick the launch file, starting in the install folder. </summary>
        [RelayCommand]
        private async Task PickLaunchFileAsync()
        {
            var start = InstallPath.Length > 0 ? InstallPath : Path.GetDirectoryName(ExecutablePath);
            var picked = await Dialogs.Current.PickLaunchFileAsync(start);
            if (picked == null) return;

            var path = GameInstall.Normalize(picked);
            if (GameInstall.IsWrongPlatform(path))
                await Dialogs.Current.AlertAsync("Файл не запуститься на цій системі",
                    "Це програма Windows. Шлях буде збережено, але запустити гру з застосунку на цій системі не вийде.");
            else if (GameInstall.NeedsExecutePermission(path))
                await InstallPrompts.AllowExecutionAsync(path);

            SetLaunchFile(path);
        }

        /// <summary> Removes the launch file. </summary>
        [RelayCommand]
        private void ClearLaunchFile() => SetLaunchFile(string.Empty);

        /// <summary> Changes the launch file. Without a folder the size was measured from this file, so it is reset. </summary>
        private void SetLaunchFile(string path)
        {
            if (GameInstall.SamePath(path, ExecutablePath)) return;

            if (InstallPath.Length == 0)
            {
                StopMeasuring();
                SizeGb = null;
            }
            ExecutablePath = path;
        }

        private bool CanMeasure => !IsMeasuring && (InstallPath.Length > 0 || ExecutablePath.Length > 0);

        /// <summary> Measures the size of the folder, or of the launch file if there is no folder. </summary>
        [RelayCommand(CanExecute = nameof(CanMeasure))]
        private async Task MeasureAsync()
        {
            var fromFolder = InstallPath.Length > 0;
            if (fromFolder ? IsFolderMissing : IsLaunchFileMissing)
            {
                await Dialogs.Current.AlertAsync(fromFolder ? "Теку не знайдено" : "Файл не знайдено",
                    fromFolder ? InstallPath : ExecutablePath);
                return;
            }

            var cts = new CancellationTokenSource();
            _measuring = cts;
            IsMeasuring = true;
            try
            {
                var size = await GameInstall.MeasureAsync(InstallPath, ExecutablePath, cts.Token);
                // The path may have changed while the last files were being read.
                if (!cts.IsCancellationRequested)
                    SizeGb = size;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Dialogs.Current.ErrorAsync("Не вдалося порахувати розмір",
                    fromFolder ? InstallPath : ExecutablePath, ex);
            }
            finally
            {
                if (_measuring == cts)
                {
                    _measuring = null;
                    IsMeasuring = false;
                }
                cts.Dispose();
            }
        }

        /// <summary> Cancels the running measurement, if any. Called on path changes and when the window closes. </summary>
        public void StopMeasuring()
        {
            _measuring?.Cancel();
            _measuring = null;
            IsMeasuring = false;
        }

        /// <summary>
        /// Builds a game from the entered data. Fields that are not on the form are kept from the edited game.
        /// A DLC gets no background, icon or links: they are hidden on its form.
        /// </summary>
        private Game BuildGame() => new Game
        {
            GameId = _source?.GameId ?? 0,
            DeveloperId = SelectedStudio!.StudioId,
            DeveloperName = SelectedStudio.StudioName,
            ParentGameId = _parentGameId,
            GameTitle = Title,
            GameDescription = Description,
            ReleaseYear = ReleaseYear,
            GameStatus = Status,
            SizeGB = SizeGb,
            PersonalRating = PersonalRating,
            CoverImagePath = StoredOrPrevious(CoverImagePath, _source?.CoverImagePath),
            BackgroundImagePath = IsDlc ? string.Empty : StoredOrPrevious(BackgroundImagePath, _source?.BackgroundImagePath),
            ExecutablePath = ExecutablePath,
            InstallPath = InstallPath,
            IconPath = IsDlc ? string.Empty : StoredOrPrevious(IconPath, _source?.IconPath),
            AddedAt = _source?.AddedAt ?? DateTime.Now,
            IsArchived = _source?.IsArchived ?? false,
            Genres = IsDlc ? new() : Genres.CheckedOptions
                .Select(o => new Genre { GenreId = o.Id, GenreName = o.Name }).ToList(),
            Platforms = IsDlc ? new() : Platforms.CheckedOptions
                .Select(o => new Platform { PlatformId = o.Id, PlatformName = o.Name }).ToList(),
            Tags = IsDlc ? new() : Tags.CheckedOptions
                .Select(o => new Tag { TagId = o.Id, TagName = o.Name }).ToList()
        };

        /// <summary> Validates the form and saves the game. On a database error the form stays open. </summary>
        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private async Task ConfirmAsync()
        {
            // Enter executes the command directly, bypassing the disabled button.
            if (!CanConfirm) return;

            Title = TextRules.NormalizeName(Title);
            Description = TextRules.NormalizeFree(Description);
            ValidateAllProperties();
            if (HasErrors)
            {
                SelectedTabIndex = 0;
                return;
            }

            var game = BuildGame();
            try
            {
                if (_source == null)
                    GameRepository.Insert(game);
                else
                    GameRepository.Update(game);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження гри", ex);
                return;
            }

            await SaveImagesAsync(game);

            SavedGame = game;
            CloseRequested?.Invoke();
        }

        /// <summary> Saving waits for the size measurement, so a half-measured size is never written. </summary>
        private bool CanConfirm => !IsMeasuring;

        /// <summary> True if an image was picked but not imported into the media folder yet. </summary>
        private static bool IsPending(string path) => path.Length > 0 && !MediaStore.IsStored(path);

        /// <summary>
        /// Path written with the game itself: a stored or cleared path as is, and for a pending image the previous one,
        /// so a failed import leaves the old image in place.
        /// </summary>
        private static string StoredOrPrevious(string path, string? previous) =>
            IsPending(path) ? previous ?? string.Empty : path;

        /// <summary> Imports a pending image into the folder of the game; other paths are returned as is. </summary>
        private static string Store(string path, int gameId, ImageKind kind) =>
            IsPending(path) ? MediaStore.ImportGameImage(path, gameId, kind) : path;

        /// <summary>
        /// Imports picked images after the game is saved (a new game gets its id only then) and writes their paths.
        /// The game stays saved on failure; the database keeps the previous images.
        /// </summary>
        private async Task SaveImagesAsync(Game game)
        {
            if (!IsPending(CoverImagePath) && !IsPending(BackgroundImagePath) && !IsPending(IconPath))
                return;

            try
            {
                var cover = Store(CoverImagePath, game.GameId, ImageKind.Cover);
                var background = Store(BackgroundImagePath, game.GameId, ImageKind.Background);
                var icon = Store(IconPath, game.GameId, ImageKind.Icon);

                GameRepository.UpdateImages(game.GameId, cover, background, icon);

                game.CoverImagePath = cover;
                game.BackgroundImagePath = background;
                game.IconPath = icon;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DatabaseException)
            {
                await Dialogs.Current.ErrorAsync("Зображення не збережено",
                    "Гру збережено, але зображення не вдалося скопіювати.", ex);
            }
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
            StopMeasuring();
            SavedGame = null;
            CloseRequested?.Invoke();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}