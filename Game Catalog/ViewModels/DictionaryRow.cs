using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of a dictionary table: an item plus the transient search-match flag. </summary>
    public partial class DictionaryRow<T> : ObservableObject where T : class
    {
        /// <summary> The item shown in this row. </summary>
        public T Item { get; }

        /// <summary> True when the row matches the current search text. </summary>
        [ObservableProperty] private bool _isMatch;

        public DictionaryRow(T item) => Item = item;
    }
}
