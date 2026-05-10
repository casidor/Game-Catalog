using CommunityToolkit.Mvvm.Input;
using System;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the universal confirmation dialog window.
    /// </summary>
    public partial class ConfirmationViewModel : ViewModelBase
    {
        public string Title { get; }
        public string Message { get; }
        public string ConfirmText { get; }
        public string CancelText { get; }
        public bool Confirmed { get; private set; }

        public event Action? CloseRequested;

        public ConfirmationViewModel(string title, string message,
            string confirmText = "Так", string cancelText = "Скасувати")
        {
            Title = title;
            Message = message;
            ConfirmText = confirmText;
            CancelText = cancelText;
        }

        [RelayCommand]
        private void Confirm() { Confirmed = true; CloseRequested?.Invoke(); }

        [RelayCommand]
        private void Cancel() { Confirmed = false; CloseRequested?.Invoke(); }
    }
}