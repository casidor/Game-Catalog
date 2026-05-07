using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// Main window ViewModel that manages page navigation.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary> Currently displayed page. </summary>
        [ObservableProperty]
        private ViewModelBase _currentPage;

        /// <summary> The game library page. </summary>
        public LibraryViewModel LibraryPage { get; } = new();

        /// <summary> The studio management page. </summary>
        public StudioViewModel StudioPage { get; } = new();

        /// <summary> The archive page. </summary>
        public ArchiveViewModel ArchivePage { get; } = new();

        /// <summary> The statistics page. </summary>
        public StatisticsViewModel StatisticsPage { get; } = new();

        /// <summary> The settings page. </summary>
        public SettingsViewModel SettingsPage { get; } = new();
        public MainWindowViewModel()
        {
            _currentPage = LibraryPage;
            LibraryPage.GameSelected += game => NavigateToGame(game, false);
            ArchivePage.GameSelected += game => NavigateToGame(game, true);
            StudioPage.StudioSelected += studio => NavigateToStudio(studio);
        }

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="page">Target page ViewModel.</param>
        [RelayCommand]
        private void NavigateTo(ViewModelBase page)
        {
            CurrentPage = page;
        }

        /// <summary>
        /// Navigates to the game detail page.
        /// </summary>
        public void NavigateToGame(Game game, bool isArchived)
        {
            var detailVm = new GameDetailsViewModel(game, isArchived);
            detailVm.BackRequested += () => CurrentPage = isArchived ? ArchivePage : LibraryPage;
            CurrentPage = detailVm;
        }
        public void NavigateToStudio(Studio studio)
        {
            var detailVm = new StudioDetailsViewModel(studio);
            detailVm.BackRequested += () => CurrentPage = StudioPage;
            CurrentPage = detailVm;
        }
    }
}
