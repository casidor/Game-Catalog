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
    /// <summary> ViewModel for the add/edit studio dialog. </summary>
    public partial class AddStudioViewModel : ViewModelBase
    {
        private const string NotSetText = "Не вказано";

        /// <summary> Name of the studio entered by the user. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _studioName = string.Empty;

        /// <summary> Chosen country. Empty means no country. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CountryButtonText))]
        private string _selectedCountry = string.Empty;

        /// <summary> Text typed in the country search box. </summary>
        [ObservableProperty]
        private string _countrySearchText = string.Empty;

        /// <summary> Year the studio was founded. Null if unknown. </summary>
        [CurrentYearRange(1950, ErrorMessage = "Некоректний рік заснування")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private int? _foundationYear;

        /// <summary> Selected main genre. Null means no genre. </summary>
        [ObservableProperty]
        private Genre? _selectedGenre;

        /// <summary> Website of the studio. </summary>
        [MaxLength(200, ErrorMessage = "Адреса сайту не може перевищувати 200 символів")]
        [NotWhiteSpace(ErrorMessage = "Адреса сайту не може складатися лише з пробілів")]
        [WebsiteUrl]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _website = string.Empty;

        /// <summary> Caption of the country button. </summary>
        public string CountryButtonText =>
            string.IsNullOrEmpty(SelectedCountry) ? NotSetText : SelectedCountry;

        /// <summary> Countries matching the search text. The "not set" entry comes first while the search is empty. </summary>
        public ObservableCollection<string> FilteredCountries { get; } = new();

        /// <summary> Genres offered in the dropdown, a null entry first. </summary>
        public ObservableCollection<Genre?> GenreOptions { get; } = new();

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly int _studioId;

        partial void OnCountrySearchTextChanged(string value) => UpdateFilteredCountries();

        /// <summary> Initializes the dialog for adding a new studio. </summary>
        public AddStudioViewModel()
        {
            WindowTitle = "Додати студію";
            UpdateFilteredCountries();
            LoadGenres();
        }

        /// <summary> Initializes the dialog for editing an existing studio. </summary>
        public AddStudioViewModel(Studio studio) : this()
        {
            WindowTitle = "Редагувати студію";
            _studioId = studio.StudioId;
            StudioName = studio.StudioName;
            SelectedCountry = studio.Country;
            FoundationYear = studio.FoundationYear;
            SelectedGenre = GenreOptions.FirstOrDefault(g => g?.GenreId == studio.MainGenre);
            Website = studio.Website;
        }

        /// <summary> Builds a studio object from the entered data. </summary>
        public Studio BuildStudio() => new Studio
        {
            StudioId = _studioId,
            StudioName = StudioName,
            Country = SelectedCountry,
            FoundationYear = FoundationYear,
            MainGenre = SelectedGenre?.GenreId,
            Website = Website
        };

        /// <summary> Rebuilds the country list for the current search text. </summary>
        private void UpdateFilteredCountries()
        {
            var query = CountrySearchText.Trim();

            FilteredCountries.Clear();
            if (query.Length == 0)
                FilteredCountries.Add(NotSetText);

            foreach (var country in Countries.All)
            {
                if (query.Length == 0 || country.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    FilteredCountries.Add(country);
            }
        }

        /// <summary> Clears the country search box. Called when the country list is opened. </summary>
        public void ResetCountrySearch()
        {
            CountrySearchText = string.Empty;
            UpdateFilteredCountries();
        }

        /// <summary> Applies the country picked in the list. The "not set" entry clears the country. </summary>
        public void PickCountry(string? country) =>
            SelectedCountry = country == null || country == NotSetText ? string.Empty : country;

        /// <summary> Reloads the genre list and keeps the current selection if it still exists. </summary>
        private void LoadGenres()
        {
            var selectedId = SelectedGenre?.GenreId;

            GenreOptions.Clear();
            GenreOptions.Add(null);
            try
            {
                foreach (var genre in GenreRepository.GetAll())
                    GenreOptions.Add(genre);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження жанрів", ex);
            }

            SelectedGenre = GenreOptions.FirstOrDefault(g => g?.GenreId == selectedId);
        }

        /// <summary> Adds a new genre from the dialog and selects it. </summary>
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

            LoadGenres();
            SelectedGenre = GenreOptions.FirstOrDefault(g => g?.GenreId == genre.GenreId);
        }

        /// <summary> Validates the form and closes the dialog. </summary>
        [RelayCommand]
        private void Confirm()
        {
            ValidateAllProperties();
            if (HasErrors) return;
            Confirmed = true;
            CloseRequested?.Invoke();
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
            Confirmed = false;
            CloseRequested?.Invoke();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}