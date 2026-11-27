using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game_Catalog.Services
{
    /// <summary> Updates a bound collection in place, so list controls keep their scroll position after a reload. </summary>
    public static class CollectionSync
    {
        /// <summary>
        /// Makes the target hold the source items in the source order through small changes: removes rows that are gone,
        /// inserts new rows, takes out and inserts again rows whose place changed and replaces the rest with their fresh
        /// copies. Unlike Clear followed by Add, a grid sees separate changes instead of a new list and does not scroll
        /// back to the top. Replaced rows lose their selection, so the caller selects them again where needed.
        /// Move is not used on purpose: the DataGrid collection view ignores Move notifications and its rows would get
        /// out of order.
        /// </summary>
        /// <param name="key"> Identity of a row, unique within the source (usually its database id). </param>
        public static void SyncWith<T, TKey>(this ObservableCollection<T> target, IReadOnlyList<T> source, Func<T, TKey> key)
            where TKey : notnull
        {
            var keys = new HashSet<TKey>();
            foreach (var item in source)
                keys.Add(key(item));

            // Remove rows that are not in the source any more; the rest of the target then matches the source by keys.
            for (var i = target.Count - 1; i >= 0; i--)
            {
                if (!keys.Contains(key(target[i])))
                    target.RemoveAt(i);
            }

            // Everything before i already matches the source, so the row for position i is searched from i on.
            for (var i = 0; i < source.Count; i++)
            {
                var index = IndexOfKey(target, key(source[i]), key, i);
                if (index == i)
                {
                    target[i] = source[i];
                    continue;
                }

                // A new row, or a row further down whose place changed: the old copy goes out and the fresh one comes in.
                if (index > i)
                    target.RemoveAt(index);
                target.Insert(i, source[i]);
            }
        }

        /// <summary>
        /// Same as SyncWith, and keeps the same rows selected (matched by key) while they are still in the source.
        /// Rows the grid selected on its own during the update are dropped: when its current row is replaced, the DataGrid
        /// moves the current row to a neighbour and selects that one.
        /// </summary>
        /// <param name="selected"> Selection bound to the list control; rebuilt from the fresh rows. </param>
        public static void SyncWith<T, TKey>(this ObservableCollection<T> target, IReadOnlyList<T> source, Func<T, TKey> key,
            ICollection<T> selected)
            where TKey : notnull
        {
            var selectedKeys = new HashSet<TKey>();
            foreach (var item in selected)
                selectedKeys.Add(key(item));

            // Clearing first saves the controls from dropping the replaced rows from the selection one by one.
            selected.Clear();
            target.SyncWith(source, key);

            // Clear again to drop whatever the grid selected during the update, then select the same rows again.
            selected.Clear();
            foreach (var item in target)
            {
                if (selectedKeys.Contains(key(item)))
                    selected.Add(item);
            }
        }

        /// <summary> Returns the index of the first row with the given key at or after start, or -1. </summary>
        private static int IndexOfKey<T, TKey>(ObservableCollection<T> target, TKey value, Func<T, TKey> key, int start)
            where TKey : notnull
        {
            var comparer = EqualityComparer<TKey>.Default;
            for (var i = start; i < target.Count; i++)
            {
                if (comparer.Equals(key(target[i]), value))
                    return i;
            }
            return -1;
        }
    }
}
