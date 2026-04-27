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
        /// <summary>
        /// Currently displayed page.
        /// </summary>
        [ObservableProperty]
        private ViewModelBase _currentPage;

        /// <summary>
        /// The game library page.
        /// </summary>
        public LibraryViewModel LibraryPage { get; } = new();

        /// <summary>
        /// The studio management page.
        /// </summary>
        public StudioViewModel StudioPage { get; } = new();

        /// <summary>
        /// The archive page.
        /// </summary>
        public ArchiveViewModel ArchivePage { get; } = new();

        public MainWindowViewModel()
        {
            _currentPage = LibraryPage;
            LibraryPage.GameSelected += NavigateToGame;
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
        /// <param name="game">The game to display.</param>
        public void NavigateToGame(Game game)
        {
            var detailVm = new GameDetailsViewModel(game);
            detailVm.BackRequested += () => CurrentPage = LibraryPage;
            CurrentPage = detailVm;
        }
    }
}
