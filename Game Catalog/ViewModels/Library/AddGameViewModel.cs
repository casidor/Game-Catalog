using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
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
        [CurrentYearRange(1970, 5, ErrorMessage = "Некоректний рік випуску")]
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

        /// <summary> Local path of the cover image. </summary>
        [ObservableProperty] private string _coverImagePath = string.Empty;

        /// <summary> Local path of the background image. </summary>
        [ObservableProperty] private string _backgroundImagePath = string.Empty;

        /// <summary> Local path of the icon. </summary>
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

        /// <summary> Index of the selected form tab (0 = details, 1 = media). </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailsTab))]
        [NotifyPropertyChangedFor(nameof(IsMediaTab))]
        private int _selectedTabIndex;

        /// <summary> True when the details tab is selected. </summary>
        public bool IsDetailsTab => SelectedTabIndex == 0;

        /// <summary> True when the media tab is selected. </summary>
        public bool IsMediaTab => SelectedTabIndex == 1;

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

        /// <summary> Initializes the dialog for editing an existing game. </summary>
        public AddGameViewModel(Game game) : this()
        {
            WindowTitle = "Редагувати гру";
            _source = game;

            Title = game.GameTitle;
            SelectedStudio = Studios.FirstOrDefault(s => s.StudioId == game.DeveloperId);
            ReleaseYear = game.ReleaseYear;
            Status = game.GameStatus;
            PersonalRating = game.PersonalRating;
            Description = game.GameDescription;
            CoverImagePath = game.CoverImagePath;
            BackgroundImagePath = game.BackgroundImagePath;
            IconPath = game.IconPath;

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
                    PlatformRepository.GetAll().Select(p => new FilterOption(p.PlatformName, p.PlatformId)), alsoCheckId);
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

            var path = await Dialogs.Current.PickImageAsync(title);
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

        /// <summary> Builds a game from the entered data. Fields that are not on the form are kept from the edited game. </summary>
        private Game BuildGame() => new Game
        {
            GameId = _source?.GameId ?? 0,
            DeveloperId = SelectedStudio!.StudioId,
            DeveloperName = SelectedStudio.StudioName,
            ParentGameId = _source?.ParentGameId,
            GameTitle = Title,
            GameDescription = Description,
            ReleaseYear = ReleaseYear,
            GameStatus = Status,
            SizeGB = _source?.SizeGB,
            PersonalRating = PersonalRating,
            CoverImagePath = CoverImagePath,
            BackgroundImagePath = BackgroundImagePath,
            ExecutablePath = _source?.ExecutablePath ?? string.Empty,
            InstallPath = _source?.InstallPath ?? string.Empty,
            IconPath = IconPath,
            AddedAt = _source?.AddedAt ?? DateTime.Now,
            IsArchived = _source?.IsArchived ?? false,
            Genres = Genres.CheckedOptions
                .Select(o => new Genre { GenreId = o.Id, GenreName = o.Name }).ToList(),
            Platforms = Platforms.CheckedOptions
                .Select(o => new Platform { PlatformId = o.Id, PlatformName = o.Name }).ToList(),
            Tags = Tags.CheckedOptions
                .Select(o => new Tag { TagId = o.Id, TagName = o.Name }).ToList()
        };

        /// <summary> Validates the form and saves the game. On a database error the form stays open. </summary>
        [RelayCommand]
        private async Task ConfirmAsync()
        {
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

            SavedGame = game;
            CloseRequested?.Invoke();
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
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