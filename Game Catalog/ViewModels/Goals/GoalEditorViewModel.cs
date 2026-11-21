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
    /// <summary>
    /// ViewModel for the add/edit goal dialog. The dialog saves the goal itself, so a failed save keeps the form open.
    /// The status is not edited here: a new goal is active, and the status changes through the list actions.
    /// </summary>
    public partial class GoalEditorViewModel : ViewModelBase
    {
        /// <summary> Game of the goal. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameButtonText))]
        private Game? _selectedGame;

        /// <summary> Short name of the goal. </summary>
        [Required(ErrorMessage = "Назва не може бути порожньою")]
        [MaxLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
        [NotWhiteSpace(ErrorMessage = "Назва не може бути лише з пробілів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _goalName = string.Empty;

        /// <summary> Free-form description. </summary>
        [MaxLength(500, ErrorMessage = "Опис не може перевищувати 500 символів")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        private string _goalDescription = string.Empty;

        /// <summary> Priorities offered in the list, 1 is the highest. </summary>
        public int[] PriorityOptions { get; } = { 1, 2, 3, 4, 5 };

        /// <summary> Chosen priority. </summary>
        [ObservableProperty] private int _selectedPriority = 3;

        /// <summary> Planned completion date. Null means no deadline. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDueDate))]
        private DateTime? _dueDate;

        /// <summary> True when a due date is set, so it can be cleared. </summary>
        public bool HasDueDate => DueDate != null;

        /// <summary> Earliest due date the calendar offers: today for a new goal, the creation day for an existing one. </summary>
        public DateTime MinDueDate { get; }

        /// <summary> Message about an incorrectly filled form, shown under the fields. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
        private string _errorMessage = string.Empty;

        /// <summary> True when there is a form message to show. </summary>
        public bool HasErrorMessage => ErrorMessage.Length > 0;

        /// <summary> All games offered in the dropdown. </summary>
        public ObservableCollection<Game> Games { get; } = new();

        /// <summary> Text typed in the game search box. </summary>
        [ObservableProperty] private string _gameSearchText = string.Empty;

        /// <summary> Games matching the search text. </summary>
        public ObservableCollection<Game> FilteredGames { get; } = new();

        /// <summary> True when the dialog is opened for a fixed game, so the game cannot be changed. </summary>
        public bool IsGameLocked { get; }

        /// <summary> True when the game can be picked from the dropdown. </summary>
        public bool IsGameEditable => !IsGameLocked;

        /// <summary> Caption of the game button. </summary>
        public string GameButtonText => SelectedGame?.GameTitle ?? "Оберіть гру";

        /// <summary> The goal as saved by the dialog, or null if the dialog was cancelled. </summary>
        public Goal? SavedGoal { get; private set; }

        /// <summary> Raised when the dialog should close. </summary>
        public event Action? CloseRequested;

        /// <summary> Title displayed in the window header. </summary>
        public string WindowTitle { get; }

        private readonly Goal? _source;

        partial void OnGameSearchTextChanged(string value) => UpdateFilteredGames();

        /// <summary> Initializes the dialog for a new goal or, if a goal is given, for editing it. </summary>
        /// <param name="goal"> Goal to edit, or null to add a new one. </param>
        /// <param name="presetGameId"> Game selected for a new goal, or null. </param>
        public GoalEditorViewModel(Goal? goal = null, int? presetGameId = null)
        {
            _source = goal;
            IsGameLocked = presetGameId != null;
            WindowTitle = goal == null ? "Додати ціль" : "Редагувати ціль";
            MinDueDate = goal?.CreatedAt.Date ?? DateTime.Today;
            LoadGames();

            if (goal == null)
            {
                SelectedGame = Games.FirstOrDefault(g => g.GameId == presetGameId);
                return;
            }

            SelectedGame = Games.FirstOrDefault(g => g.GameId == goal.GameId);
            GoalName = goal.GoalName;
            GoalDescription = goal.GoalDescription;
            SelectedPriority = goal.Priority;
            DueDate = goal.DueDate;
        }

        /// <summary> Loads all games for the dropdown. </summary>
        private void LoadGames()
        {
            try
            {
                foreach (var game in GameRepository.GetAll(new GameFilter { IsArchived = false }))
                    Games.Add(game);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження ігор", ex);
            }
            UpdateFilteredGames();
        }

        /// <summary> Rebuilds the game list for the current search text. </summary>
        private void UpdateFilteredGames()
        {
            var query = GameSearchText.Trim();

            FilteredGames.Clear();
            foreach (var game in Games)
            {
                if (query.Length == 0 || game.GameTitle.Contains(query, StringComparison.OrdinalIgnoreCase))
                    FilteredGames.Add(game);
            }
        }

        /// <summary> Adds a new game with the game editor and selects it. The editor saves the game itself. </summary>
        [RelayCommand]
        private async Task AddGameAsync()
        {
            var game = await Dialogs.Current.ShowGameEditorAsync();
            if (game == null) return;

            Games.Clear();
            LoadGames();
            SelectedGame = Games.FirstOrDefault(g => g.GameId == game.GameId);
        }

        /// <summary> Applies the game picked in the list. </summary>
        public void PickGame(Game game) => SelectedGame = game;

        /// <summary> Removes the due date, leaving the goal without a deadline. </summary>
        [RelayCommand]
        private void ClearDueDate() => DueDate = null;

        /// <summary> Checks what the database cannot explain well. Returns a message, or null if the form is complete. </summary>
        private string? Validate()
        {
            if (SelectedGame == null)
                return "Оберіть гру";
            if (DueDate is { } due && due.Date < MinDueDate)
                return _source == null
                    ? "Термін не може бути раніше за сьогодні"
                    : $"Термін не може бути раніше дня створення цілі ({MinDueDate:dd.MM.yyyy})";
            return null;
        }

        /// <summary> Validates the form and saves the goal. On a database error the form stays open. </summary>
        [RelayCommand]
        private async Task ConfirmAsync()
        {
            ValidateAllProperties();
            if (HasErrors) return;

            ErrorMessage = Validate() ?? string.Empty;
            if (HasErrorMessage) return;

            var goal = new Goal
            {
                GoalId = _source?.GoalId ?? 0,
                GameId = SelectedGame!.GameId,
                GameTitle = SelectedGame.GameTitle,
                GoalName = GoalName,
                GoalDescription = GoalDescription,
                Priority = SelectedPriority,
                DueDate = DueDate?.Date
            };

            try
            {
                if (_source == null)
                    GoalRepository.Insert(goal);
                else
                    GoalRepository.Update(goal);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження цілі", ex);
                return;
            }

            SavedGoal = goal;
            CloseRequested?.Invoke();
        }

        /// <summary> Cancels the dialog. </summary>
        [RelayCommand]
        private void Cancel()
        {
            SavedGoal = null;
            CloseRequested?.Invoke();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}