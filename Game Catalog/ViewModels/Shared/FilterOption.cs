using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> A checkbox entry of a filter list: a country (by name) or a genre (by id). </summary>
    public sealed partial class FilterOption : ObservableObject
    {
        /// <summary> Display text; for countries it is also the filter value. </summary>
        public string Name { get; }

        /// <summary> Identifier of the genre. Unused for countries. </summary>
        public int Id { get; }

        /// <summary> Whether the option is ticked in the filter panel. </summary>
        [ObservableProperty] private bool _isChecked;

        public FilterOption(string name, int id = 0)
        {
            Name = name;
            Id = id;
        }
    }
}