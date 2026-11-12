using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Threading.Tasks;

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
        public GamesListViewModel LibraryPage { get; } = new(isArchive: false);

        /// <summary> The studio management page. </summary>
        public StudioViewModel StudioPage { get; } = new();

        /// <summary> The play sessions page. </summary>
        public SessionsViewModel SessionsPage { get; } = new();

        /// <summary> The archive page. </summary>
        public GamesListViewModel ArchivePage { get; } = new(isArchive: true);

        /// <summary> The dictionaries page. </summary>
        public DictionariesViewModel DictionariesPage { get; } = new();

        /// <summary> The settings page. </summary>
        public SettingsViewModel SettingsPage { get; } = new();
        public MainWindowViewModel()
        {
            _currentPage = LibraryPage;
            StudioPage.StudioSelected += studio => NavigateToStudio(studio);
            LibraryPage.GameSelected += game => NavigateToGame(game, LibraryPage);
            ArchivePage.GameSelected += game => NavigateToGame(game, ArchivePage);
        }

        /// <summary> Raised when the application cannot continue and the window must close. </summary>
        public event Action? CloseRequested;

        /// <summary> Creates the database if needed and shows the first page. Called once after the window is loaded. </summary>
        public void Start()
        {
            try { DatabaseInitializer.Initialize(); }
            catch (Exception ex)
            {
                _ = FailStartAsync(ex);
                return;
            }

            CurrentPage.OnNavigatedTo();
        }

        private async Task FailStartAsync(Exception ex)
        {
            await Dialogs.Current.ErrorAsync("Помилка бази даних",
                "Не вдалося ініціалізувати або прочитати базу даних. Застосунок буде закрито.", ex);
            CloseRequested?.Invoke();
        }

        /// <summary> Disposes the previous page when navigating away from it. </summary>
        partial void OnCurrentPageChanging(ViewModelBase value)
        {
            if (CurrentPage is IDisposable disposable && !ReferenceEquals(CurrentPage, value))
                disposable.Dispose();
        }

        /// <summary> Lets the page that was just shown load its data. </summary>
        partial void OnCurrentPageChanged(ViewModelBase value) => value.OnNavigatedTo();

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="page">Target page ViewModel.</param>
        [RelayCommand]
        private void NavigateTo(ViewModelBase page)
        {
            CurrentPage = page;
        }

        /// <summary> Navigates to the game details page. "Back" returns to the page the user came from. </summary>
        public void NavigateToGame(Game game, ViewModelBase backPage)
        {
            var detailVm = new GameDetailsViewModel(game);
            detailVm.BackRequested += () => CurrentPage = backPage;
            CurrentPage = detailVm;
        }

        /// <summary> Navigates to the studio details page. </summary>
        public void NavigateToStudio(Studio studio)
        {
            var detailVm = new StudioDetailsViewModel(studio);
            detailVm.BackRequested += () => CurrentPage = StudioPage;
            CurrentPage = detailVm;
        }
    }
}
