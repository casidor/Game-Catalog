using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary> A multi-select dropdown field: options with checkboxes, a search box and a summary caption. </summary>
    public sealed partial class MultiSelectField : ObservableObject
    {
        private readonly string _emptyText;
        private readonly List<FilterOption> _all = new();

        /// <summary> Options matching the current search text. </summary>
        public ObservableCollection<FilterOption> FilteredOptions { get; } = new();

        /// <summary> Text typed in the search box of the dropdown. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> Ticked options. </summary>
        public IEnumerable<FilterOption> CheckedOptions => _all.Where(o => o.IsChecked);

        /// <summary> Caption of the dropdown button: nothing, one name, or the first two names and a counter. </summary>
        public string Summary
        {
            get
            {
                var names = CheckedOptions.Select(o => o.Name).ToList();
                return names.Count switch
                {
                    0 => _emptyText,
                    <= 2 => string.Join(", ", names),
                    _ => $"{names[0]}, {names[1]} +{names.Count - 2}"
                };
            }
        }

        public MultiSelectField(string emptyText) => _emptyText = emptyText;

        partial void OnSearchTextChanged(string value) => ApplySearch();

        /// <summary> Replaces the options and keeps the ticks of those that still exist, optionally ticking one more. </summary>
        public void Refill(IEnumerable<FilterOption> items, int? alsoCheckId = null)
        {
            var checkedIds = CheckedOptions.Select(o => o.Id).ToHashSet();
            if (alsoCheckId != null) checkedIds.Add(alsoCheckId.Value);

            foreach (var old in _all)
                old.PropertyChanged -= OnOptionChanged;
            _all.Clear();

            foreach (var item in items)
            {
                item.IsChecked = checkedIds.Contains(item.Id);
                item.PropertyChanged += OnOptionChanged;
                _all.Add(item);
            }

            ApplySearch();
            OnPropertyChanged(nameof(Summary));
        }

        /// <summary> Ticks exactly the options with the given ids. </summary>
        public void SetChecked(IEnumerable<int> ids)
        {
            var set = ids.ToHashSet();
            foreach (var option in _all)
                option.IsChecked = set.Contains(option.Id);
        }

        private void OnOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterOption.IsChecked))
                OnPropertyChanged(nameof(Summary));
        }

        private void ApplySearch()
        {
            var query = SearchText.Trim();

            FilteredOptions.Clear();
            foreach (var option in _all)
            {
                if (query.Length == 0 || option.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    FilteredOptions.Add(option);
            }
        }
    }
}