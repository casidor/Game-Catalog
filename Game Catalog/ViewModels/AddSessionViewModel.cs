using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the add/edit play session dialog. </summary>
    public partial class AddSessionViewModel : ViewModelBase
    {
        /// <summary> Game this session belongs to. </summary>
        [Required(ErrorMessage = "Оберіть гру")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private Game? _selectedGame;

        /// <summary> Date the session started. </summary>
        [Required(ErrorMessage = "Вкажіть дату початку")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private DateTime? _startDate = DateTime.Today;

        /// <summary> Time of day the session started. </summary>
        [Required(ErrorMessage = "Вкажіть час початку")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private TimeSpan? _startTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

        /// <summary> Date the session ended. Null together with EndTime means ongoing. </summary>
        [ObservableProperty]
        private DateTime? _endDate;

        /// <summary> Time of day the session ended. </summary>
        [ObservableProperty]
        private TimeSpan? _endTime;

        /// <summary> How the session was added. </summary>
        [ObservableProperty]
        private EntryMethod _entryMethod = EntryMethod.Manual;

        /// <summary> Optional note about the session. </summary>
        [MaxLength(500, ErrorMessage = "Нотатка не може перевищувати 500 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _note = string.Empty;

        /// <summary> Cross-field validation message. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFormError))]
        private string _formError = string.Empty;

        /// <summary> True if there is a cross-field validation message. </summary>
        public bool HasFormError => !string.IsNullOrEmpty(FormError);

        /// <summary> Games available for selection. </summary>
        public ObservableCollection<Game> Games { get; }

        /// <summary> Available entry methods. </summary>
        public EntryMethod[] Methods { get; } = Enum.GetValues<EntryMethod>();

        /// <summary> Indicates whether the user confirmed the dialog. </summary>
        public bool Confirmed { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        public AddSessionViewModel(ObservableCollection<Game> games)
        {
            Games = games;
            WindowTitle = "Додати сесію";
        }

        /// <summary> Initializes the dialog with an existing session for editing. </summary>
        public AddSessionViewModel(PlaySession session, ObservableCollection<Game> games) : this(games)
        {
            WindowTitle = "Редагувати сесію";
            SelectedGame = session.Game ?? games.FirstOrDefault(g => g.Id == session.GameId);
            StartDate = session.StartTime.Date;
            StartTime = session.StartTime.TimeOfDay;
            EndDate = session.EndTime?.Date;
            EndTime = session.EndTime?.TimeOfDay;
            EntryMethod = session.EntryMethod;
            Note = session.Note;
        }

        private static DateTime? Combine(DateTime? date, TimeSpan? time) =>
            date.HasValue && time.HasValue ? date.Value.Date + time.Value : null;

        /// <summary> Builds a session object from the entered data. </summary>
        public PlaySession BuildSession() => new PlaySession
        {
            Game = SelectedGame,
            GameId = SelectedGame?.Id ?? 0,
            StartTime = Combine(StartDate, StartTime)!.Value,
            EndTime = Combine(EndDate, EndTime),
            EntryMethod = EntryMethod,
            Note = Note
        };

        /// <summary> Clears the end date and time, marking the session as ongoing. </summary>
        [RelayCommand]
        private void ClearEnd()
        {
            EndDate = null;
            EndTime = null;
            FormError = string.Empty;
        }

        /// <summary> Validates the form and closes the dialog. </summary>
        [RelayCommand]
        private void Confirm()
        {
            ValidateAllProperties();
            if (HasErrors) return;

            if (EndDate.HasValue != EndTime.HasValue)
            {
                FormError = "Вкажіть і дату, і час завершення або очистіть обидва поля";
                return;
            }

            var start = Combine(StartDate, StartTime)!.Value;
            var end = Combine(EndDate, EndTime);
            if (end.HasValue && end.Value < start)
            {
                FormError = "Час завершення не може бути раніше початку";
                return;
            }

            FormError = string.Empty;
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