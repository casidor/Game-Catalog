using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the add game dialog window.
    /// </summary>
    public partial class AddGameViewModel : ViewModelBase
    {
        /// <summary>
        /// Game title entered by the user.
        /// </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто короткая")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [MaxLength(200, ErrorMessage = "Назва не може перевищувати 200 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Selected developer studio.
        /// </summary>
        [Required(ErrorMessage = "Оберіть студію-розробника")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private Studio? _selectedStudio;

        /// <summary>
        /// Game genre entered by the user.
        /// </summary>
        [Required(ErrorMessage = "Жанр не може бути порожнім")]
        [MinLength(2, ErrorMessage = "Жанр занадто короткий")]
        [NotWhiteSpace(ErrorMessage = "Жанр не може бути лише з пробілів")]
        [MaxLength(100, ErrorMessage = "Жанр не може перевищувати 100 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _genre = string.Empty;

        /// <summary>
        /// Release year entered by the user.
        /// </summary>
        [CurrentYearRange(1970, ErrorMessage = "Некоректний рік випуску")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int _releaseYear = DateTime.Now.Year;

        /// <summary>
        /// Platform entered by the user.
        /// </summary>
        [Required(ErrorMessage = "Платформа не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Платформа занадто короткая")]
        [NotWhiteSpace(ErrorMessage = "Платформа не може бути лише з пробілів")]
        [MaxLength(200, ErrorMessage = "Платформа не може перевищувати 200 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _platform = string.Empty;

        /// <summary>
        /// Disk size in GB entered by the user.
        /// </summary>
        [Range(0.01, 1000.0, ErrorMessage = "Розмір має бути від 0.01 до 1000 ГБ")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private double _sizeGB;

        /// <summary>
        /// Current game status selected by the user.
        /// </summary>
        [ObservableProperty]
        private GameStatus _status = GameStatus.NotInstalled;

        /// <summary>
        /// Hours played entered by the user.
        /// </summary>
        [Range(0.0, 100000.0, ErrorMessage = "Години не можуть бути від'ємними")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private double _hoursPlayed;

        /// <summary>
        /// Personal rating entered by the user (1 to 10).
        /// </summary>
        [Range(1, 10, ErrorMessage = "Оцінка від 1 до 10")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int _personalRating = 5;

        /// <summary>Plain-text description fetched from RAWG.</summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>Local file path of the downloaded cover image.</summary>
        [ObservableProperty]
        private string _coverImagePath = string.Empty;

        /// <summary>Indicates whether the RAWG suggestion dropdown is visible.</summary>
        [ObservableProperty]
        private bool _isSuggestionsVisible;

        /// <summary>Indicates whether a RAWG search is currently in progress.</summary>
        [ObservableProperty]
        private bool _isSearching;

        /// <summary>List of game suggestions returned by RAWG search.</summary>
        public ObservableCollection<RawgGameResult> Suggestions { get; } = new();

        /// <summary>
        /// List of available studios for selection.
        /// </summary>
        public ObservableCollection<Studio> Studios { get; }

        /// <summary>
        /// List of available game statuses.
        /// </summary>
        public Array Statuses { get; } = Enum.GetValues(typeof(GameStatus));

        /// <summary>
        /// Indicates whether the user confirmed the dialog.
        /// </summary>
        public bool Confirmed { get; private set; }
        /// <summary>
        /// Event triggered when the dialog should be closed.
        /// </summary>
        public event Action? CloseRequested;

        /// <summary>
        /// Title displayed in the window header.
        /// </summary>
        public string WindowTitle { get; }

        private bool _isInitializing;
        private CancellationTokenSource? _searchCts;

        public AddGameViewModel(ObservableCollection<Studio> studios)
        {
            Studios = studios;
            WindowTitle = "Додати гру";
        }

        /// <summary>Builds and returns the game object from entered data.</summary>
        public Game BuildGame() => new Game
        {
            Title = Title,
            Developer = SelectedStudio,
            Genre = Genre,
            ReleaseYear = ReleaseYear,
            Platform = Platform,
            SizeGB = SizeGB,
            Status = Status,
            HoursPlayed = HoursPlayed,
            PersonalRating = PersonalRating,
            Description = Description,
            CoverImagePath = CoverImagePath
        };

        /// <summary>
        /// Initializes the view model with an existing game for editing.
        /// </summary>
        /// <param name="game">The game to edit.</param>
        /// <param name="studios">Available studios collection.</param>
        public AddGameViewModel(Game game, ObservableCollection<Studio> studios)
        {
            WindowTitle = "Редагувати гру";
            _isInitializing = true;
            Studios = studios;
            Title = game.Title;
            SelectedStudio = game.Developer;
            Genre = game.Genre;
            ReleaseYear = game.ReleaseYear;
            Platform = game.Platform;
            SizeGB = game.SizeGB;
            Status = game.Status;
            HoursPlayed = game.HoursPlayed;
            PersonalRating = game.PersonalRating;
            _isInitializing = true;
            Description = game.Description;
            CoverImagePath = game.CoverImagePath;
            _isInitializing = false;
        }

        /// <summary>
        /// Confirms the dialog and signals the window to close.
        /// </summary>
        [RelayCommand]
        private void Confirm()
        {
            ValidateAllProperties();
            if (HasErrors) return;
            Confirmed = true;
            CloseRequested?.Invoke();
        }

        /// <summary>
        /// Cancels the dialog.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            Confirmed = false;
            CloseRequested?.Invoke();
        }

        /// <summary>Triggers a debounced RAWG search when the title field changes.</summary>
        partial void OnTitleChanged(string value)
        {
            if (_isInitializing) return;
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            _ = SearchRawgAsync(value, _searchCts.Token);
        }

        /// <summary>Searches RAWG with a 400ms debounce and populates the suggestions list.</summary>
        private async Task SearchRawgAsync(string query, CancellationToken ct)
        {
            if (!RawgService.IsAvailable) return;

            try { await Task.Delay(400, ct); }
            catch (TaskCanceledException) { return; }

            IsSearching = true;
            var results = await RawgService.SearchAsync(query, ct);
            IsSearching = false;

            if (ct.IsCancellationRequested) return;

            Suggestions.Clear();
            foreach (var r in results)
                Suggestions.Add(r);

            IsSuggestionsVisible = Suggestions.Count > 0;
        }

        /// <summary>Selects a suggestion, fills all available fields from RAWG detail endpoint, and downloads the cover.</summary>
        [RelayCommand]
        public async Task SelectSuggestionAsync(RawgGameResult result)
        {
            _searchCts?.Cancel();
            _isInitializing = true;
            Title = result.Name;
            _isInitializing = false;

            IsSuggestionsVisible = false;
            Suggestions.Clear();

            IsSearching = true;
            var detail = await RawgService.GetDetailAsync(result.Id);
            IsSearching = false;

            if (detail == null) return;

            if (detail.Genres.Count > 0)
                Genre = string.Join(", ", detail.Genres.Select(g => g.Name));
            if (detail.Platforms.Count > 0)
                Platform = string.Join(", ", detail.Platforms.Select(p => p.Platform.Name));
            if (detail.Developers.Count > 0) ; 
            if (!string.IsNullOrWhiteSpace(detail.DescriptionRaw))
                Description = detail.DescriptionRaw;
            if (DateTime.TryParse(detail.Released, out var date))
                ReleaseYear = date.Year;
            if (!string.IsNullOrWhiteSpace(detail.BackgroundImage))
                CoverImagePath = await RawgService.DownloadCoverAsync(detail.BackgroundImage, detail.Id);
        }

        /// <summary>Hides the suggestion dropdown without selecting anything.</summary>
        [RelayCommand]
        public void DismissSuggestions()
        {
            IsSuggestionsVisible = false;
            Suggestions.Clear();
        }
    }
}
