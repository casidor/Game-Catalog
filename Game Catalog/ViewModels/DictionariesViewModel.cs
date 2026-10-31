using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the dictionaries page with one tab per dictionary table. </summary>
    public partial class DictionariesViewModel : ViewModelBase
    {
        /// <summary> Genres tab. </summary>
        public GenresViewModel Genres { get; } = new();

        /// <summary> Platforms tab. </summary>
        public PlatformsViewModel Platforms { get; } = new();

        /// <summary> Index of the selected tab (0 = genres, 1 = platforms). </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGenresTab))]
        [NotifyPropertyChangedFor(nameof(IsPlatformsTab))]
        private int _selectedTabIndex;

        /// <summary> True when the genres tab is selected. </summary>
        public bool IsGenresTab => SelectedTabIndex == 0;

        /// <summary> True when the platforms tab is selected. </summary>
        public bool IsPlatformsTab => SelectedTabIndex == 1;

        /// <summary> Loads data of all tabs. Called when the page is shown. </summary>
        public void Load()
        {
            Genres.Reload();
            Platforms.Reload();
        }
    }
}