using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// User-facing texts of a dictionary tab. Item forms are in the accusative case,
    /// as in "Видалити жанр", "Видалити 3 жанри", "Видалити 5 жанрів".
    /// The blocked texts are for items in use; a dictionary whose items never block leaves them empty.
    /// </summary>
    public sealed record DictionaryTexts(
        string LoadError,
        string SearchError,
        string SaveError,
        string DeleteError,
        string SearchPlaceholder,
        string AddTip,
        string ItemOne,
        string ItemFew,
        string ItemMany,
        string DeleteTitleOne,
        string DeleteTitleMany,
        string DeleteNote = "",
        Func<string, string>? BlockedOne = null,
        string BlockedManyIntro = "",
        string BlockedManyHint = "",
        string SkippedIntro = "");

    /// <summary>
    /// Base ViewModel of a dictionary tab: list, sorting, search highlight, selection of several rows,
    /// adding, editing and deleting. Sorting, search matching and the "in use" check are done by the database
    /// through the repository calls of derived classes.
    /// </summary>
    public abstract partial class DictionaryTabViewModel<T> : ViewModelBase, IDictionaryTab where T : class
    {
        /// <summary> Rows of the table, in the order returned by the database. </summary>
        public ObservableCollection<DictionaryRow<T>> Rows { get; } = new();

        /// <summary> Rows selected in the table, mirrored from the grid by DataGridBehavior. </summary>
        public ObservableCollection<DictionaryRow<T>> SelectedRows { get; } = new();

        /// <summary> Single selected row of the grid; set here to select an item again after it was edited. </summary>
        [ObservableProperty] private DictionaryRow<T>? _selectedRow;

        /// <summary> Text whose matches are highlighted in the table. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> "Found N of M" text shown next to the search box; empty when nothing is searched. </summary>
        [ObservableProperty] private string _searchSummary = string.Empty;

        /// <summary> Field of the current sort order; the sorting itself is done by the database. </summary>
        [ObservableProperty] private DictionarySortField _sortField = DictionarySortField.Name;

        /// <summary> True for descending order. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> True when the dictionary has no items. </summary>
        public bool IsEmpty => Rows.Count == 0;

        /// <summary> Context menu caption with the number of selected items. </summary>
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        public string SearchPlaceholder => Texts.SearchPlaceholder;

        public string AddTip => Texts.AddTip;

        /// <summary> Messages shown by this tab. </summary>
        protected abstract DictionaryTexts Texts { get; }

        /// <summary> Loads all items from the repository in the given order. </summary>
        protected abstract List<T> LoadItems(DictionarySortField field, bool descending);

        /// <summary> Returns ids of the items matching the search text; the match is decided by the database. </summary>
        protected abstract HashSet<int> LoadMatchIds(string search);

        /// <summary> Returns which of the given items are still in use and cannot be deleted. </summary>
        protected abstract HashSet<int> LoadIdsInUse(IReadOnlyCollection<int> ids);

        /// <summary> Identifier of the item, used to mark rows found by the search. </summary>
        protected abstract int GetId(T item);

        /// <summary> Creates the table row for the item. </summary>
        protected virtual DictionaryRow<T> CreateRow(T item) => new(item);

        /// <summary> Opens the editor dialog and returns the edited item, or null if cancelled. </summary>
        protected abstract Task<T?> ShowEditorAsync(T? item);

        /// <summary> Inserts a new item into the repository. </summary>
        protected abstract void InsertItem(T item);

        /// <summary> Updates an existing item in the repository. </summary>
        protected abstract void UpdateItem(T item);

        /// <summary> Deletes the items with the given ids, all or none. </summary>
        protected abstract void DeleteItems(IReadOnlyCollection<int> ids);

        /// <summary> Display name of the item used in messages. </summary>
        protected abstract string GetName(T item);

        protected DictionaryTabViewModel()
        {
            SelectedRows.CollectionChanged += (_, _) => OnSelectionChanged();
        }

        partial void OnSearchTextChanged(string value) => UpdateMatches();

        /// <summary> Refreshes the menu caption and the commands that depend on the selection. </summary>
        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(DeleteSelectedHeader));
            EditSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary> Sorts by the clicked table column: same column flips the direction. </summary>
        public void SortByColumn(DictionarySortField field)
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

        /// <summary> Reloads the table from the database, already in the chosen order. </summary>
        public void Reload()
        {
            try
            {
                var items = LoadItems(SortField, SortDescending);
                Rows.Clear();
                foreach (var item in items)
                    Rows.Add(CreateRow(item));
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync(Texts.LoadError, ex);
            }

            UpdateMatches();
            OnPropertyChanged(nameof(IsEmpty));
        }

        /// <summary> Marks rows found by the database search and updates the "found" counter. </summary>
        private void UpdateMatches()
        {
            // Nothing to search for or nothing to mark: reset without asking the database.
            if (string.IsNullOrWhiteSpace(SearchText) || Rows.Count == 0)
            {
                foreach (var row in Rows)
                    row.IsMatch = false;
                SearchSummary = string.Empty;
                return;
            }

            HashSet<int> matches;
            try
            {
                matches = LoadMatchIds(TextRules.NormalizeName(SearchText));
            }
            catch (DatabaseException ex)
            {
                matches = new HashSet<int>();
                _ = ShowErrorAsync(Texts.SearchError, ex);
            }

            foreach (var row in Rows)
                row.IsMatch = matches.Contains(GetId(row.Item));

            SearchSummary = $"Знайдено: {Rows.Count(r => r.IsMatch)} з {Rows.Count}";
        }

        /// <summary> Opens the editor and saves a new item. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var item = await ShowEditorAsync(null);
            if (item == null) return;

            try
            {
                InsertItem(item);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(Texts.SaveError, ex);
                return;
            }
            Reload();
        }

        /// <summary> Opens the editor for the item of a double-clicked row and selects it again after saving. </summary>
        [RelayCommand]
        private async Task EditAsync(DictionaryRow<T> row)
        {
            var edited = await ShowEditorAsync(row.Item);
            if (edited == null) return;

            try
            {
                UpdateItem(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(Texts.SaveError, ex);
                return;
            }
            Reload();

            var id = GetId(row.Item);
            SelectedRow = Rows.FirstOrDefault(r => GetId(r.Item) == id);
        }

        /// <summary> Opens the editor for the only selected item. </summary>
        [RelayCommand(CanExecute = nameof(HasSingleSelection))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0]);

        private bool HasSingleSelection() => SelectedRows.Count == 1;

        /// <summary>
        /// Deletes the selected items after one confirmation. Items still in use cannot be deleted,
        /// so they are named in the confirmation and skipped; the rest are deleted together.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task DeleteSelectedAsync()
        {
            var rows = SelectedRows.ToList();

            HashSet<int> inUse;
            try
            {
                inUse = LoadIdsInUse(rows.Select(r => GetId(r.Item)).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(Texts.DeleteError, ex);
                return;
            }

            var blocked = rows.Where(r => inUse.Contains(GetId(r.Item))).ToList();
            var free = rows.Where(r => !inUse.Contains(GetId(r.Item))).ToList();

            // Nothing can be deleted: only explain why.
            if (free.Count == 0)
            {
                await Dialogs.Current.AlertAsync(
                    $"Неможливо видалити {(rows.Count == 1 ? Texts.ItemOne : Texts.ItemFew)}",
                    rows.Count == 1
                        ? Texts.BlockedOne!(GetName(rows[0].Item))
                        : $"{Texts.BlockedManyIntro}\n{NamesList(blocked)}\n\n{Texts.BlockedManyHint}");
                return;
            }

            var question = free.Count == 1
                ? $"Видалити {Texts.ItemOne} «{GetName(free[0].Item)}»?"
                : blocked.Count == 0
                    ? $"Видалити {Plural.Count(free.Count, Texts.ItemOne, Texts.ItemFew, Texts.ItemMany)}?"
                    : $"Видалити решту {Plural.Count(free.Count, Texts.ItemOne, Texts.ItemFew, Texts.ItemMany)}?";
            var skipped = blocked.Count == 0
                ? string.Empty
                : $"{Texts.SkippedIntro}\n{NamesList(blocked)}\n\n";
            var note = Texts.DeleteNote.Length > 0 ? $"\n{Texts.DeleteNote}" : string.Empty;

            var confirmed = await Dialogs.Current.ConfirmAsync(
                free.Count == 1 ? Texts.DeleteTitleOne : Texts.DeleteTitleMany,
                $"{skipped}{question}{note}\nЦю дію неможливо скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                DeleteItems(free.Select(r => GetId(r.Item)).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(Texts.DeleteError, ex);
                return;
            }
            Reload();

            // The skipped items stay selected, so it is clear which ones are left.
            var skippedIds = blocked.Select(r => GetId(r.Item)).ToHashSet();
            foreach (var row in Rows.Where(r => skippedIds.Contains(GetId(r.Item))))
                SelectedRows.Add(row);
        }

        private bool HasSelection() => SelectedRows.Count > 0;

        /// <summary> Names of the items as a bulleted list; a long list is cut after the first few names. </summary>
        private string NamesList(IReadOnlyList<DictionaryRow<T>> rows)
        {
            const int shown = 10;
            var lines = rows.Take(shown).Select(r => $"• {GetName(r.Item)}");
            var text = string.Join("\n", lines);
            return rows.Count > shown ? $"{text}\n…і ще {rows.Count - shown}" : text;
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
