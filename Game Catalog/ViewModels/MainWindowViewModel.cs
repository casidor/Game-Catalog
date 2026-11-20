using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
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

        /// <summary> The goals page. </summary>
        public GoalsViewModel GoalsPage { get; } = new();

        /// <summary> The statistics page. </summary>
        public StatisticsViewModel StatisticsPage { get; } = new();

        /// <summary> The archive page. </summary>
        public GamesListViewModel ArchivePage { get; } = new(isArchive: true);

        /// <summary> The dictionaries page. </summary>
        public DictionariesViewModel DictionariesPage { get; } = new();

        /// <summary> The settings page. </summary>
        public SettingsViewModel SettingsPage { get; } = new();

        /// <summary> Sidebar buttons of the main sections, top to bottom. </summary>
        public IReadOnlyList<NavItem> NavItems { get; }

        /// <summary> Sidebar button of the settings, pinned to the bottom. </summary>
        public NavItem SettingsItem { get; }

        /// <summary> True when the sidebar shows captions next to the icons. </summary>
        [ObservableProperty]
        private bool _isSidebarExpanded;

        /// <summary> Icon of the theme toggle: the theme a click switches to. </summary>
        public string ThemeToggleIcon => SettingsPage.SelectedTheme == "Light" ? "🌙" : "☀";

        public MainWindowViewModel()
        {
            _currentPage = LibraryPage;
            _isSidebarExpanded = SettingsService.Current.SidebarExpanded;

            NavItems = new[]
            {
                new NavItem("🎮", "Бібліотека", LibraryPage) { IsActive = true },
                new NavItem("🏢", "Студії", StudioPage),
                new NavItem("⏱", "Сесії", SessionsPage),
                new NavItem("🎯", "Цілі", GoalsPage),
                new NavItem("📊", "Статистика", StatisticsPage),
                new NavItem("📦", "Архів", ArchivePage),
                new NavItem("🏷", "Довідники", DictionariesPage)
            };
            SettingsItem = new NavItem("⚙", "Налаштування", SettingsPage);

            // The theme can also be changed on the settings page: keep the toggle icon in step with it.
            SettingsPage.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.SelectedTheme))
                    OnPropertyChanged(nameof(ThemeToggleIcon));
            };

            StudioPage.StudioSelected += studio => NavigateToStudio(studio, StudioPage);
            LibraryPage.GameSelected += game => NavigateToGame(game, LibraryPage);
            ArchivePage.GameSelected += game => NavigateToGame(game, ArchivePage);
            StatisticsPage.GameSelected += gameId => OpenGame(gameId, StatisticsPage);
            StatisticsPage.StudioSelected += studioId => OpenStudio(studioId, StatisticsPage);
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

        /// <summary> Remembers the chosen sidebar mode between application runs. </summary>
        partial void OnIsSidebarExpandedChanged(bool value)
        {
            SettingsService.Current.SidebarExpanded = value;
            SettingsService.Save();
        }

        /// <summary> Switches the sidebar between the narrow (icons only) and the wide mode. </summary>
        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

        /// <summary> Switches between the dark and the light theme. The settings page applies and saves it. </summary>
        [RelayCommand]
        private void ToggleTheme() =>
            SettingsPage.SelectedTheme = SettingsPage.SelectedTheme == "Light" ? "Dark" : "Light";

        /// <summary>
        /// Navigates to the page of a sidebar section and marks that section as active.
        /// Details pages are opened from a section, so they keep its button highlighted.
        /// </summary>
        /// <param name="page">Target page ViewModel.</param>
        [RelayCommand]
        private void NavigateTo(ViewModelBase page)
        {
            CurrentPage = page;

            foreach (var item in NavItems)
                item.IsActive = ReferenceEquals(item.Page, page);
            SettingsItem.IsActive = ReferenceEquals(SettingsItem.Page, page);
        }

        /// <summary> Navigates to the game details page. "Back" returns to the page the user came from. </summary>
        public void NavigateToGame(Game game, ViewModelBase backPage)
        {
            var detailVm = new GameDetailsViewModel(game);
            detailVm.BackRequested += () => CurrentPage = backPage;
            CurrentPage = detailVm;
        }

        /// <summary>
        /// Opens the details of a game known only by its id.
        /// Reloads the back page instead when the game no longer exists.
        /// </summary>
        private void OpenGame(int gameId, ViewModelBase backPage)
        {
            Game? game;
            try
            {
                game = GameRepository.GetById(gameId);
            }
            catch (DatabaseException ex)
            {
                _ = Dialogs.Current.ErrorAsync("Помилка завантаження гри", ex.Message, ex);
                return;
            }

            if (game == null)
            {
                backPage.OnNavigatedTo();
                return;
            }

            NavigateToGame(game, backPage);
        }

        /// <summary>
        /// Opens the details of a studio known only by its id.
        /// Reloads the back page instead when the studio no longer exists.
        /// </summary>
        private void OpenStudio(int studioId, ViewModelBase backPage)
        {
            Studio? studio;
            try
            {
                studio = StudioRepository.GetById(studioId);
            }
            catch (DatabaseException ex)
            {
                _ = Dialogs.Current.ErrorAsync("Помилка завантаження студії", ex.Message, ex);
                return;
            }

            if (studio == null)
            {
                backPage.OnNavigatedTo();
                return;
            }

            NavigateToStudio(studio, backPage);
        }

        /// <summary> Navigates to the studio details page. "Back" returns to the page the user came from. </summary>
        public void NavigateToStudio(Studio studio, ViewModelBase backPage)
        {
            var detailVm = new StudioDetailsViewModel(studio);
            detailVm.BackRequested += () => CurrentPage = backPage;
            CurrentPage = detailVm;
        }
    }
}
