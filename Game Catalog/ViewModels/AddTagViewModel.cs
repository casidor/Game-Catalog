using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit tag dialog. </summary>
    public partial class AddTagViewModel : ViewModelBase
    {
        /// <summary> Name of the tag entered by the user. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MinLength(2, ErrorMessage = "Назва занадто коротка")]
        [MaxLength(50, ErrorMessage = "Назва не може перевищувати 50 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _tagName = string.Empty;

        /// <summary> Selected palette color. Null means the tag has no color. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoColor))]
        [NotifyPropertyChangedFor(nameof(SelectedColorName))]
        private TagColor? _selectedColor;

        /// <summary> Palette colors offered in the dialog. </summary>
        public IReadOnlyList<TagColor> Colors => TagPalette.Colors;

        /// <summary> True when no palette color is selected. </summary>
        public bool HasNoColor => SelectedColor == null;

        /// <summary> Name of the selected color, or a label for no color. </summary>
        public string SelectedColorName => SelectedColor?.Name ?? "Без кольору";

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly int _tagId;

        /// <summary> Initializes the dialog for adding a new tag. </summary>
        public AddTagViewModel()
        {
            WindowTitle = "Додати тег";
        }

        /// <summary> Initializes the dialog for editing an existing tag. </summary>
        public AddTagViewModel(Tag tag)
        {
            WindowTitle = "Редагувати тег";
            _tagId = tag.TagId;
            TagName = tag.TagName;
            SelectedColor = TagPalette.Find(tag.Color);
        }

        /// <summary> Builds a tag object from the entered data. </summary>
        public Tag BuildTag() => new Tag
        {
            TagId = _tagId,
            TagName = TagName,
            Color = SelectedColor?.Key ?? string.Empty
        };

        /// <summary> Removes the color selection. </summary>
        [RelayCommand]
        private void ClearColor() => SelectedColor = null;

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