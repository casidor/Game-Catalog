using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Threading.Tasks;

namespace Game_Catalog.ViewModels
{
    /// <summary> Sessions tab of the game details: the sessions of one game with a short summary above them. </summary>
    public partial class GameSessionsTabViewModel : ObservableObject
    {
        private readonly int _gameId;

        /// <summary>
        /// Raised after sessions of the game were added, changed or deleted. The details page reloads the game,
        /// since its total play time depends on the sessions, and passes the new total back through TotalHours.
        /// </summary>
        public event Action? SessionsChanged;

        /// <summary> Sessions table of the game; read-only for an archived game. </summary>
        public SessionTableViewModel Table { get; }

        /// <summary> Total play time of the game, as calculated by the database for the game details. </summary>
        [ObservableProperty] private double _totalHours;

        /// <summary> True when sessions can be added: the game is not archived. </summary>
        public bool CanAdd => !Table.IsReadOnly;

        /// <summary> True when the game has no sessions at all. </summary>
        public bool IsEmpty => Table.Rows.Count == 0;

        /// <summary> Number of sessions shown in the summary. </summary>
        public int SessionCount => Table.Rows.Count;

        public GameSessionsTabViewModel(Game game)
        {
            _gameId = game.GameId;
            _totalHours = game.TotalHours;
            Table = new SessionTableViewModel(
                (sortField, descending) => SessionRepository.GetByGame(_gameId, sortField, descending),
                isReadOnly: game.IsArchived);
            Table.DataChanged += OnDataChanged;
        }

        /// <summary> Loads the sessions of the game and refreshes the summary. Called every time the tab is opened. </summary>
        public void Reload()
        {
            Table.Reload();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SessionCount));
        }

        private void OnDataChanged()
        {
            Reload();
            SessionsChanged?.Invoke();
        }

        /// <summary> Opens the editor for a new session of this game; the game cannot be changed there. </summary>
        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAsync()
        {
            var saved = await Dialogs.Current.ShowSessionEditorAsync(presetGameId: _gameId);
            if (saved != null)
                OnDataChanged();
        }
    }
}
