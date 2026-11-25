using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// DLC tab of the game details: the DLC of one game with adding, editing and deleting.
    /// DLC of an archived game may only be viewed.
    /// </summary>
    public partial class GameDlcTabViewModel : ObservableObject
    {
        private readonly Game _game;

        /// <summary> True when the DLC may only be viewed: the game is archived. </summary>
        public bool IsReadOnly { get; }

        /// <summary> True when DLC can be added: the game is not archived. </summary>
        public bool CanAdd => !IsReadOnly;

        /// <summary> Rows of the table, oldest DLC first. </summary>
        public ObservableCollection<DlcRow> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<DlcRow> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select a DLC again after it was saved. </summary>
        [ObservableProperty] private DlcRow? _selectedRow;

        /// <summary> True when the game has no DLC. </summary>
        public bool IsEmpty => Rows.Count == 0;

        /// <summary> Number of DLC, e.g. "Доповнень: 3". </summary>
        public string SummaryText => $"Доповнень: {Rows.Count}";

        /// <summary> Context menu caption with the number of DLC to delete. </summary>
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        public GameDlcTabViewModel(Game game)
        {
            _game = game;
            IsReadOnly = game.IsArchived;
            SelectedRows.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(DeleteSelectedHeader));
                EditSelectedCommand.NotifyCanExecuteChanged();
                DeleteSelectedCommand.NotifyCanExecuteChanged();
            };
        }

        /// <summary> Loads the DLC of the game. Called every time the tab is opened and after changes. </summary>
        public void Reload()
        {
            try
            {
                var dlcs = GameRepository.GetDlcs(_game.GameId);
                Rows.Clear();
                foreach (var dlc in dlcs)
                    Rows.Add(new DlcRow(dlc));
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження доповнень", ex);
            }
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SummaryText));
        }

        /// <summary> Opens the editor for a new DLC of this game and selects it after saving. </summary>
        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task AddAsync()
        {
            var saved = await Dialogs.Current.ShowNewDlcEditorAsync(_game);
            if (saved == null) return;

            Reload();
            SelectedRow = Rows.FirstOrDefault(r => r.Dlc.GameId == saved.GameId);
        }

        /// <summary> Opens the editor for the DLC of a double-clicked row and selects it again after saving. </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private async Task EditAsync(DlcRow row)
        {
            var saved = await Dialogs.Current.ShowGameEditorAsync(row.Dlc);
            if (saved == null) return;

            Reload();
            SelectedRow = Rows.FirstOrDefault(r => r.Dlc.GameId == saved.GameId);
        }

        /// <summary> Opens the editor for the only selected DLC. </summary>
        [RelayCommand(CanExecute = nameof(CanEditSelected))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0]);

        private bool CanEdit(DlcRow row) => !IsReadOnly;

        private bool CanEditSelected() => !IsReadOnly && SelectedRows.Count == 1;

        /// <summary> Deletes the selected DLC in one transaction after confirmation. </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelectedAsync()
        {
            var rows = SelectedRows.ToList();
            var confirmed = rows.Count == 1
                ? await Dialogs.Current.ConfirmAsync(
                    "Видалення доповнення",
                    $"Видалити доповнення «{rows[0].Title}»?\nЦю дію неможливо скасувати.",
                    "Видалити")
                : await Dialogs.Current.ConfirmAsync(
                    "Видалення доповнень",
                    $"Видалити {DlcCount(rows.Count)}?\nЦю дію неможливо скасувати.",
                    "Видалити");
            if (!confirmed) return;

            try
            {
                GameRepository.DeleteDlcs(rows.Select(r => r.Dlc.GameId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення доповнень", ex);
                return;
            }
            Reload();
        }

        private bool HasSelection() => !IsReadOnly && SelectedRows.Count > 0;

        /// <summary> Number of DLC with the matching Ukrainian word form: "1 доповнення", "3 доповнення", "5 доповнень". </summary>
        private static string DlcCount(int count)
        {
            var word = (count % 10, count % 100) switch
            {
                (1, not 11) => "доповнення",
                (>= 2 and <= 4, not (>= 12 and <= 14)) => "доповнення",
                _ => "доповнень"
            };
            return $"{count} {word}";
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
