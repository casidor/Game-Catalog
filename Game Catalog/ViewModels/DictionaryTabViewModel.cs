using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> User-facing messages of a dictionary tab. </summary>
    public sealed record DictionaryTexts(
        string LoadError,
        string SaveError,
        string DeleteError,
        string DeleteTitle,
        Func<string, string> DeleteMessage,
        string BlockedTitle,
        Func<string, string> BlockedMessage);

    /// <summary>
    /// Base ViewModel of a dictionary tab: search, list, add, edit and delete flow.
    /// Derived classes provide repository calls, the editor dialog and the texts.
    /// </summary>
    public abstract partial class DictionaryTabViewModel<T> : ViewModelBase where T : class
    {
        /// <summary> Items matching the current search. </summary>
        public ObservableCollection<T> Items { get; } = new();

        /// <summary> Text used to search items. </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary> True when there are no items at all. </summary>
        public bool IsEmpty => Items.Count == 0 && string.IsNullOrWhiteSpace(SearchText);

        /// <summary> True when the search found nothing. </summary>
        public bool IsFilteredEmpty => Items.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

        /// <summary> Messages shown by this tab. </summary>
        protected abstract DictionaryTexts Texts { get; }

        /// <summary> Loads items matching the search text from the repository. </summary>
        protected abstract List<T> LoadItems(string search);

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

        partial void OnSearchTextChanged(string value) => Reload();

        /// <summary> Reloads the list from the database using the current search. </summary>
        public void Reload()
        {
            try
            {
                var items = LoadItems(SearchText);
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync(Texts.LoadError, ex);
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
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