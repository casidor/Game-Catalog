using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    /// <summary> User-facing messages of a dictionary tab. </summary>
    public sealed record DictionaryTexts(
        string LoadError,
        string SearchError,
        string SaveError,
        string DeleteError,
        string DeleteTitle,
        Func<string, string> DeleteMessage,
        string BlockedTitle,
        Func<string, string> BlockedMessage);

    /// <summary>
    /// Base ViewModel of a dictionary tab: list, sorting, search highlight, add, edit and delete flow.
    /// Sorting and search matching are done by the database through the repository calls of derived classes.
    /// </summary>
    public abstract partial class DictionaryTabViewModel<T> : ViewModelBase where T : class
    {
        /// <summary> Rows of the table, in the order returned by the database. </summary>
        public ObservableCollection<DictionaryRow<T>> Rows { get; } = new();

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

        /// <summary> Messages shown by this tab. </summary>
        protected abstract DictionaryTexts Texts { get; }

        /// <summary> Loads all items from the repository in the given order. </summary>
        protected abstract List<T> LoadItems(DictionarySortField field, bool descending);

        /// <summary> Returns ids of the items matching the search text; the match is decided by the database. </summary>
        protected abstract HashSet<int> LoadMatchIds(string search);

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

        /// <summary> Deletes the item. Returns false if it is still referenced elsewhere. </summary>
        protected abstract bool DeleteItem(T item);

        /// <summary> Display name of the item used in confirmation messages. </summary>
        protected abstract string GetName(T item);

        partial void OnSearchTextChanged(string value) => UpdateMatches();

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
                matches = LoadMatchIds(SearchText);
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

        /// <summary> Opens the editor for the given item and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync(T item)
        {
            var edited = await ShowEditorAsync(item);
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
        }

        /// <summary> Deletes the item after confirmation unless it is still in use. </summary>
        [RelayCommand]
        private async Task DeleteAsync(T item)
        {
            var name = GetName(item);
            var confirmed = await Dialogs.Current.ConfirmAsync(
                Texts.DeleteTitle, Texts.DeleteMessage(name), "Видалити");
            if (!confirmed) return;

            bool deleted;
            try
            {
                deleted = DeleteItem(item);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync(Texts.DeleteError, ex);
                return;
            }

            if (!deleted)
            {
                await Dialogs.Current.AlertAsync(Texts.BlockedTitle, Texts.BlockedMessage(name));
                return;
            }
            Reload();
        }

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}