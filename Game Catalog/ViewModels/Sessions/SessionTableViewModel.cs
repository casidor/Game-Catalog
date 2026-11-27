using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Sessions table shared by the sessions page and the sessions tab of the game details: rows and sorting done
    /// by the database, selection, editing and deleting. The owner decides which sessions are loaded.
    /// </summary>
    public partial class SessionTableViewModel : ObservableObject
    {
        /// <summary> Loads the sessions of the owner in the given order. </summary>
        private readonly Func<SessionSortField, bool, List<PlaySession>> _loadSessions;

        /// <summary>
        /// Raised after sessions were changed or deleted here. The owner reloads the table (and whatever else it shows)
        /// through Reload; the table restores the selection afterwards where it needs to.
        /// </summary>
        public event Action? DataChanged;

        /// <summary> True when the sessions may only be viewed (sessions of an archived game): no edits or deletes. </summary>
        public bool IsReadOnly { get; }

        /// <summary> Rows of the table. </summary>
        public ObservableCollection<PlaySession> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<PlaySession> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select a session again after it was edited. </summary>
        [ObservableProperty] private PlaySession? _selectedRow;

        /// <summary> Field of the current sort order; the sorting itself is done by the database. </summary>
        [ObservableProperty] private SessionSortField _sortField = SessionSortField.Start;

        /// <summary> True for descending order. The table opens with the newest sessions first. </summary>
        [ObservableProperty] private bool _sortDescending = true;

        /// <summary> Context menu caption with the number of selected sessions. </summary>
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        /// <param name="loadSessions"> Loads the sessions to show, already sorted by the given field and direction. </param>
        /// <param name="isReadOnly"> True to only view the sessions. </param>
        public SessionTableViewModel(Func<SessionSortField, bool, List<PlaySession>> loadSessions, bool isReadOnly = false)
        {
            _loadSessions = loadSessions;
            IsReadOnly = isReadOnly;
            SelectedRows.CollectionChanged += (_, _) => OnSelectionChanged();
        }

        /// <summary> Refreshes the menu caption and the commands that depend on the selection. </summary>
        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(DeleteSelectedHeader));
            EditSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary> Sorts by the clicked table column: same column flips the direction. </summary>
        public void SortByColumn(SessionSortField field)
        {
            if (SortField == field)
            {
                SortDescending = !SortDescending;
            }
            else
            {
                SortField = field;
                SortDescending = false;
            }
            Reload();
        }

        /// <summary> Loads the sessions of the owner from the database, already in the chosen order. </summary>
        public void Reload()
        {
            try
            {
                var sessions = _loadSessions(SortField, SortDescending);
                Rows.SyncWith(sessions, s => s.PlaySessionId, SelectedRows);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження сесій", ex);
            }
        }

        /// <summary> Opens the editor for the session of a double-clicked row and selects that session again after saving. </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private async Task EditAsync(PlaySession session)
        {
            var saved = await Dialogs.Current.ShowSessionEditorAsync(session);
            if (saved == null)
                return;

            DataChanged?.Invoke();
            SelectedRow = Rows.FirstOrDefault(r => r.PlaySessionId == session.PlaySessionId);
        }

        private bool CanEdit(PlaySession session) => !IsReadOnly;

        /// <summary> Opens the editor for the only selected session. </summary>
        [RelayCommand(CanExecute = nameof(CanEditSelected))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0]);

        private bool CanEditSelected() => !IsReadOnly && SelectedRows.Count == 1;

        /// <summary> Deletes the selected sessions in one transaction after confirmation. </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelectedAsync()
        {
            var sessions = SelectedRows.ToList();
            var confirmed = sessions.Count == 1
                ? await Dialogs.Current.ConfirmAsync(
                    "Видалення сесії",
                    $"Видалити сесію гри «{sessions[0].GameTitle}» від {sessions[0].StartTime:dd.MM.yyyy HH:mm}?\n" +
                    "Цю дію неможливо скасувати.",
                    "Видалити")
                : await Dialogs.Current.ConfirmAsync(
                    "Видалення сесій",
                    $"Видалити {Plural.Count(sessions.Count, "сесію", "сесії", "сесій")}?\nЦю дію неможливо скасувати.",
                    "Видалити");
            if (!confirmed) return;

            try
            {
                SessionRepository.Delete(sessions.Select(s => s.PlaySessionId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення сесій", ex);
                return;
            }
            DataChanged?.Invoke();
        }

        private bool HasSelection() => !IsReadOnly && SelectedRows.Count > 0;

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
