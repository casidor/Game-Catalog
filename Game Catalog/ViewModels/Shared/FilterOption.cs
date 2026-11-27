using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> A checkbox entry of a filter list: a country (by name) or a dictionary item (by id), optionally with an icon. </summary>
    public sealed partial class FilterOption : ObservableObject
    {
        /// <summary> Display text; for countries it is also the filter value. </summary>
        public string Name { get; }

        /// <summary> Identifier of the item. Unused for countries. </summary>
        public int Id { get; }

        /// <summary> Stored icon path of the item (platforms only). Empty means no icon. </summary>
        public string IconPath { get; }

        /// <summary> Whether the option is ticked in the filter panel. </summary>
        [ObservableProperty] private bool _isChecked;

        public FilterOption(string name, int id = 0, string iconPath = "")
        {
            Name = name;
            Id = id;
            IconPath = iconPath;
        }
    }
}