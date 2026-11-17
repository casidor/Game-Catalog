using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> Goals tab of the game details: the goals of one game with a short summary above them. </summary>
    public partial class GameGoalsTabViewModel : ObservableObject
    {
        private readonly int _gameId;

        /// <summary> Goals table of the game; read-only for an archived game. </summary>
        public GoalTableViewModel Table { get; }

        /// <summary> True when goals can be added: the game is not archived. </summary>
        public bool CanAdd => !Table.IsReadOnly;

        /// <summary> True when the game has no goals at all. </summary>
        public bool IsEmpty => Table.Rows.Count == 0;

        /// <summary> Counts of the goals by status, e.g. "Активних: 2 • Невиконаних: 1 • Виконано: 5". Overdue ones only when there are any. </summary>
        public string SummaryText
        {
            get
            {
                var active = Table.Rows.Count(r => r.IsActive);
                var overdue = Table.Rows.Count(r => r.IsOverdue);
                var completed = Table.Rows.Count(r => r.IsCompleted);
                return overdue > 0
                    ? $"Активних: {active} • Невиконаних: {overdue} • Виконано: {completed}"
                    : $"Активних: {active} • Виконано: {completed}";
            }
        }

        public GameGoalsTabViewModel(Game game)
        {
            _gameId = game.GameId;
            Table = new GoalTableViewModel(
                (sortField, descending) => GoalRepository.GetByGame(_gameId, sortField, descending),
                isReadOnly: game.IsArchived);
            Table.DataChanged += Reload;
        }

        /// <summary> Loads the goals of the game and refreshes the summary. Called every time the tab is opened. </summary>
        public void Reload()
        {
            Table.Reload();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SummaryText));
        }

        /// <summary> Opens the editor for a new goal of this game; the game cannot be changed there. </summary>
        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAsync()
        {
            var saved = await Dialogs.Current.ShowGoalEditorAsync(presetGameId: _gameId);
            if (saved != null)
                Reload();
        }
    }
}
