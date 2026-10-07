using CommunityToolkit.Mvvm.Input;
using System;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the universal confirmation and alert dialog window.
    /// </summary>
    public partial class ConfirmationViewModel : ViewModelBase
    {
        /// <summary>Dialog title displayed at the top.</summary>
        public string Title { get; }

        /// <summary>Main message body of the dialog.</summary>
        public string Message { get; }

        /// <summary>Label for the confirm button.</summary>
        public string ConfirmText { get; }

        /// <summary>Label for the cancel button.</summary>
        public string CancelText { get; }

        /// <summary>Indicates whether the user confirmed the dialog.</summary>
        public bool Confirmed { get; private set; }

        /// <summary>
        /// When true, hides the cancel button and renders the dialog as a single-button alert.
        /// </summary>
        public bool IsAlert { get; }

        /// <summary>Raised when the dialog should close.</summary>
        public event Action? CloseRequested;

        /// <summary>
        /// Initializes a confirmation dialog with two buttons.
        /// </summary>
        public ConfirmationViewModel(string title, string message,
            string confirmText = "Так", string cancelText = "Скасувати")
        {
            Title = title;
            Message = message;
            ConfirmText = confirmText;
            CancelText = cancelText;
            IsAlert = false;
        }

        /// <summary>
        /// Initializes an alert dialog with a single OK button.
        /// </summary>
        public ConfirmationViewModel(string title, string message, bool isAlert)
        {
            Title = title;
            Message = message;
            ConfirmText = "OK";
            CancelText = string.Empty;
            IsAlert = isAlert;
        }

        [RelayCommand]
        private void Confirm() { Confirmed = true; CloseRequested?.Invoke(); }

        [RelayCommand]
        private void Cancel() { Confirmed = false; CloseRequested?.Invoke(); }
    }
}