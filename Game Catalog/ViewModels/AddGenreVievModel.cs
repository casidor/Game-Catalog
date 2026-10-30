using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit genre dialog. </summary>
    public partial class AddGenreViewModel : ViewModelBase
    {
        /// <summary> Name of the genre entered by the user. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _genreName = string.Empty;

        /// <summary> Optional description of the genre. </summary>
        [MaxLength(500, ErrorMessage = "Опис не може перевищувати 500 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _genreDescription = string.Empty;

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly int _genreId;

        /// <summary> Initializes the dialog for adding a new genre. </summary>
        public AddGenreViewModel()
        {
            WindowTitle = "Додати жанр";
        }

        /// <summary> Initializes the dialog for editing an existing genre. </summary>
        public AddGenreViewModel(Genre genre)
        {
            WindowTitle = "Редагувати жанр";
            _genreId = genre.GenreId;
            GenreName = genre.GenreName;
            GenreDescription = genre.GenreDescription;
        }

        /// <summary> Builds a genre object from the entered data. </summary>
        public Genre BuildGenre() => new Genre
        {
            GenreId = _genreId,
            GenreName = GenreName,
            GenreDescription = GenreDescription
        };

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
    }
}