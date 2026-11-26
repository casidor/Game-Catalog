using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Converters;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using Game_Catalog.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel of the games page. One class serves both the library and the archive. </summary>
    public partial class GamesListViewModel : ViewModelBase
    {
        private readonly bool _isArchive;
        private GameFilter _applied = new();
        private List<Game> _loaded = new();

        /// <summary> Page heading. </summary>
        public string PageTitle { get; }

        /// <summary> Heading of the empty state. </summary>
        public string EmptyTitle { get; }

        /// <summary> Hint of the empty state. </summary>
        public string EmptyHint { get; }

        /// <summary> Rows shown in the grid and in the list. </summary>
        public ObservableCollection<GameRow> Rows { get; } = new();

        /// <summary>
        /// Selected games, shared by the cards and the table: the cards bind to it directly, the table through
        /// DataGridBehavior, so the selection stays when the view is switched. The menu actions work with it.
        /// </summary>
        public ObservableCollection<GameRow> SelectedRows { get; } = new();

        public ObservableCollection<FilterOption> GenreOptions { get; } = new();
        public ObservableCollection<FilterOption> PlatformOptions { get; } = new();
        public ObservableCollection<FilterOption> TagOptions { get; } = new();
        public ObservableCollection<FilterOption> DeveloperOptions { get; } = new();
        public ObservableCollection<FilterOption> StatusOptions { get; } = new();

        /// <summary> Text whose matches are highlighted. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> "Found N of M" text shown next to the search box; empty when nothing is searched. </summary>
        [ObservableProperty] private string _searchSummary = string.Empty;

        /// <summary> Field of the current sort order. </summary>
        [ObservableProperty] private GameSortField _sortField = GameSortField.Title;

        /// <summary> True for descending order. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> True for the cover grid, false for the table. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ViewModeIcon))]
        [NotifyPropertyChangedFor(nameof(ShowGrid))]
        [NotifyPropertyChangedFor(nameof(ShowList))]
        private bool _isGridView = true;

        [ObservableProperty] private bool _isFilterPanelOpen;

        /// <summary> True when the preview panel is shown on the right of the list. </summary>
        [ObservableProperty] private bool _isPreviewOpen = true;

        /// <summary>
        /// Row whose game is shown in the preview panel: the first selected one, null when nothing is selected.
        /// Worked out from SelectedRows, so the selection has a single source.
        /// </summary>
        public GameRow? SelectedRow => SelectedRows.Count > 0 ? SelectedRows[0] : null;

        /// <summary> True when a game is selected. </summary>
        public bool HasSelection => SelectedRow != null;

        [ObservableProperty] private int? _draftYearFrom;
        [ObservableProperty] private int? _draftYearTo;
        [ObservableProperty] private int? _draftRatingFrom;
        [ObservableProperty] private int? _draftRatingTo;
        [ObservableProperty] private double? _draftSizeFrom;
        [ObservableProperty] private double? _draftSizeTo;
        [ObservableProperty] private double? _draftHoursFrom;
        [ObservableProperty] private double? _draftHoursTo;

        /// <summary> Icon of the view mode button: the mode shown right now. </summary>
        public string ViewModeIcon => IsGridView ? "▦" : "☰";

        /// <summary> Shows games as a cover grid. </summary>
        [RelayCommand]
        private void SetGridView() => IsGridView = true;

        /// <summary> Shows games as a table. </summary>
        [RelayCommand]
        private void SetListView() => IsGridView = false;

        /// <summary> Sorts by the clicked table column: same column flips the direction. </summary>
        public void SortByColumn(GameSortField field)
        {
            if (SortField == field)
            {
                SortDescending = !SortDescending;
            }
            else
            {
                SortField = field;
                SortDescending = false;
            }
            Reload();
        }

        public bool HasActiveFilter => !_applied.IsEmpty;

        /// <summary> Number of filter groups that are currently applied. </summary>
        public int ActiveFilterCount =>
            (_applied.GenreIds.Count > 0 ? 1 : 0) +
            (_applied.PlatformIds.Count > 0 ? 1 : 0) +
            (_applied.TagIds.Count > 0 ? 1 : 0) +
            (_applied.DeveloperIds.Count > 0 ? 1 : 0) +
            (_applied.Statuses.Count > 0 ? 1 : 0) +
            (_applied.YearFrom != null || _applied.YearTo != null ? 1 : 0) +
            (_applied.RatingFrom != null || _applied.RatingTo != null ? 1 : 0) +
            (_applied.SizeFrom != null || _applied.SizeTo != null ? 1 : 0) +
            (_applied.HoursFrom != null || _applied.HoursTo != null ? 1 : 0);

        public string FiltersButtonText =>
            ActiveFilterCount > 0 ? $"⚙ Фільтри · {ActiveFilterCount}" : "⚙ Фільтри";

        public bool IsEmpty => Rows.Count == 0 && !HasActiveFilter;
        public bool IsFilteredEmpty => Rows.Count == 0 && HasActiveFilter;
        public bool ShowGrid => IsGridView && Rows.Count > 0;
        public bool ShowList => !IsGridView && Rows.Count > 0;

        /// <summary> True on the archive page: the menu offers restoring instead of archiving. </summary>
        public bool IsArchive => _isArchive;

        /// <summary> Context menu captions with the number of selected games. </summary>
        public string ArchiveSelectedHeader => $"В архів ({SelectedRows.Count})";
        public string RestoreSelectedHeader => $"Відновити ({SelectedRows.Count})";
        public string DeleteSelectedHeader => $"Видалити ({SelectedRows.Count})";

        /// <summary> Raised when the user opens a game. </summary>
        public event Action<Game>? GameSelected;

        public GamesListViewModel(bool isArchive)
        {
            _isArchive = isArchive;
            PageTitle = isArchive ? "Архів" : "Моя бібліотека";
            EmptyTitle = isArchive ? "Архів порожній" : "Бібліотека порожня";
            EmptyHint = isArchive ? "Заархівовані ігри з'являться тут" : "Додайте першу гру";

            foreach (var status in Enum.GetValues<GameStatus>())
            {
                var name = GameStatusConverter.Instance
                    .Convert(status, typeof(string), null, CultureInfo.InvariantCulture) as string;
                StatusOptions.Add(new FilterOption(name ?? status.ToString(), (int)status));
            }

            SelectedRows.CollectionChanged += (_, _) => OnSelectionChanged();
        }

        /// <summary> Refreshes the menu captions and the commands that depend on the selection. </summary>
        private void OnSelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ArchiveSelectedHeader));
            OnPropertyChanged(nameof(RestoreSelectedHeader));
            OnPropertyChanged(nameof(DeleteSelectedHeader));
            EditSelectedCommand.NotifyCanExecuteChanged();
            ArchiveSelectedCommand.NotifyCanExecuteChanged();
            RestoreSelectedCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }

        partial void OnSearchTextChanged(string value) => UpdateMatches();

        /// <summary> Loads the page data when the page is shown. </summary>
        public override void OnNavigatedTo() => Load();

        /// <summary> Refreshes filter lists and the games. Called when the page is shown. </summary>
        public void Load()
        {
            IsFilterPanelOpen = false;
            try
            {
                var genres = GenreRepository.GetAll();
                var platforms = PlatformRepository.GetAll();
                var tags = TagRepository.GetAll();
                var studios = StudioRepository.GetAll(new StudioFilter());

                // Drop selections whose dictionary item was deleted meanwhile.
                _applied = _applied with
                {
                    GenreIds = _applied.GenreIds.Where(id => genres.Any(x => x.GenreId == id)).ToList(),
                    PlatformIds = _applied.PlatformIds.Where(id => platforms.Any(x => x.PlatformId == id)).ToList(),
                    TagIds = _applied.TagIds.Where(id => tags.Any(x => x.TagId == id)).ToList(),
                    DeveloperIds = _applied.DeveloperIds.Where(id => studios.Any(x => x.StudioId == id)).ToList()
                };

                Fill(GenreOptions, genres.Select(x => new FilterOption(x.GenreName, x.GenreId)));
                Fill(PlatformOptions, platforms.Select(x => new FilterOption(x.PlatformName, x.PlatformId)));
                Fill(TagOptions, tags.Select(x => new FilterOption(x.TagName, x.TagId)));
                Fill(DeveloperOptions, studios.Select(x => new FilterOption(x.StudioName, x.StudioId)));

                SyncDraftFromApplied();
            }
            catch (DatabaseException ex)
            {
                _ = ShowErrorAsync("Помилка завантаження фільтрів", ex);
            }

            Reload();
        }

        private static void Fill(ObservableCollection<FilterOption> target, IEnumerable<FilterOption> items)
        {
            target.Clear();
            foreach (var item in items)
                target.Add(item);
        }

        /// <summary> Loads games matching the applied filter from the database, already in the chosen order. </summary>
        private void Reload()
        {
            try
            {
                _loaded = GameRepository.GetAll(_applied with
                {
                    IsArchived = _isArchive,
                    SortField = SortField,
                    SortDescending = SortDescending
                });
            }
            catch (DatabaseException ex)
            {
                _loaded = new List<Game>();
                _ = ShowErrorAsync("Помилка завантаження ігор", ex);
            }

            Rebuild();
        }

        /// <summary> Rebuilds the rows from the loaded games, keeping the order returned by the database. </summary>
        private void Rebuild()
        {
            var selectedIds = SelectedRows.Select(r => r.Game.GameId).ToHashSet();

            Rows.Clear();
            foreach (var game in _loaded)
                Rows.Add(new GameRow(game));

            // Keep the same games selected if they are still in the list.
            foreach (var row in Rows.Where(r => selectedIds.Contains(r.Game.GameId)))
                SelectedRows.Add(row);

            UpdateMatches();

            OnPropertyChanged(nameof(HasActiveFilter));
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(FiltersButtonText));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
            OnPropertyChanged(nameof(ShowGrid));
            OnPropertyChanged(nameof(ShowList));
            OnPropertyChanged(nameof(ShowAddInEmpty));
        }

        /// <summary> Marks rows found by the database search and updates the "found" counter. </summary>
        private void UpdateMatches()
        {
            // Nothing to search for or nothing to mark: reset without asking the database.
            if (string.IsNullOrWhiteSpace(SearchText) || Rows.Count == 0)
            {
                foreach (var row in Rows)
                    row.IsMatch = false;
                SearchSummary = string.Empty;
                return;
            }

            HashSet<int> matches;
            try
            {
                matches = GameRepository.GetMatchIds(TextRules.NormalizeName(SearchText));
            }
            catch (DatabaseException ex)
            {
                matches = new HashSet<int>();
                _ = ShowErrorAsync("Помилка пошуку ігор", ex);
            }

            foreach (var row in Rows)
                row.IsMatch = matches.Contains(row.Game.GameId);

            SearchSummary = $"Знайдено: {Rows.Count(r => r.IsMatch)} з {Rows.Count}";
        }

        private void SyncDraftFromApplied()
        {
            foreach (var o in GenreOptions) o.IsChecked = _applied.GenreIds.Contains(o.Id);
            foreach (var o in PlatformOptions) o.IsChecked = _applied.PlatformIds.Contains(o.Id);
            foreach (var o in TagOptions) o.IsChecked = _applied.TagIds.Contains(o.Id);
            foreach (var o in DeveloperOptions) o.IsChecked = _applied.DeveloperIds.Contains(o.Id);
            foreach (var o in StatusOptions) o.IsChecked = _applied.Statuses.Contains((GameStatus)o.Id);

            DraftYearFrom = _applied.YearFrom;
            DraftYearTo = _applied.YearTo;
            DraftRatingFrom = _applied.RatingFrom;
            DraftRatingTo = _applied.RatingTo;
            DraftSizeFrom = _applied.SizeFrom;
            DraftSizeTo = _applied.SizeTo;
            DraftHoursFrom = _applied.HoursFrom;
            DraftHoursTo = _applied.HoursTo;
        }

        /// <summary> Selects the sort field. </summary>
        [RelayCommand]
        private void SetSortField(GameSortField field)
        {
            SortField = field;
            Reload();
        }

        /// <summary> Sets ascending order. </summary>
        [RelayCommand]
        private void SetAscending()
        {
            SortDescending = false;
            Reload();
        }

        /// <summary> Sets descending order. </summary>
        [RelayCommand]
        private void SetDescending()
        {
            SortDescending = true;
            Reload();
        }

        /// <summary> Opens the filter panel showing the currently applied values. </summary>
        [RelayCommand]
        private void OpenFilters()
        {
            SyncDraftFromApplied();
            IsFilterPanelOpen = true;
        }

        /// <summary> Applies the panel values, closes the panel and reloads the list. </summary>
        [RelayCommand]
        private void ApplyFilters()
        {
            _applied = new GameFilter
            {
                GenreIds = GenreOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                PlatformIds = PlatformOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                TagIds = TagOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                DeveloperIds = DeveloperOptions.Where(o => o.IsChecked).Select(o => o.Id).ToList(),
                Statuses = StatusOptions.Where(o => o.IsChecked).Select(o => (GameStatus)o.Id).ToList(),
                YearFrom = DraftYearFrom,
                YearTo = DraftYearTo,
                RatingFrom = DraftRatingFrom,
                RatingTo = DraftRatingTo,
                SizeFrom = DraftSizeFrom,
                SizeTo = DraftSizeTo,
                HoursFrom = DraftHoursFrom,
                HoursTo = DraftHoursTo
            };
            IsFilterPanelOpen = false;
            Reload();
        }

        /// <summary> Closes the panel and discards unapplied changes. </summary>
        [RelayCommand]
        private void CancelFilters() => IsFilterPanelOpen = false;

        /// <summary> Clears the panel controls without touching the applied filter or the list. </summary>
        [RelayCommand]
        private void ClearDraft()
        {
            foreach (var o in GenreOptions) o.IsChecked = false;
            foreach (var o in PlatformOptions) o.IsChecked = false;
            foreach (var o in TagOptions) o.IsChecked = false;
            foreach (var o in DeveloperOptions) o.IsChecked = false;
            foreach (var o in StatusOptions) o.IsChecked = false;

            DraftYearFrom = DraftYearTo = null;
            DraftRatingFrom = DraftRatingTo = null;
            DraftSizeFrom = DraftSizeTo = null;
            DraftHoursFrom = DraftHoursTo = null;
        }

        /// <summary> Removes the applied filter and resets the panel. </summary>
        [RelayCommand]
        private void ResetFilters()
        {
            _applied = new GameFilter();
            SyncDraftFromApplied();
            IsFilterPanelOpen = false;
            Reload();
        }
        /// <summary> True when games can be added from this page (the library, not the archive). </summary>
        public bool CanAdd => !_isArchive;

        /// <summary> True when the empty state should offer the add button. </summary>
        public bool ShowAddInEmpty => IsEmpty && !_isArchive;

        /// <summary> Opens the editor for a new game; the editor saves it, then the page reloads. </summary>
        [RelayCommand]
        private async Task AddAsync()
        {
            var game = await Dialogs.Current.ShowGameEditorAsync();
            if (game == null) return;
            Load();
        }

        /// <summary> Opens the editor for the given game; the editor saves it, then the page reloads. </summary>
        [RelayCommand]
        private async Task EditAsync(Game game)
        {
            var edited = await Dialogs.Current.ShowGameEditorAsync(game);
            if (edited == null) return;
            Load();
        }
        /// <summary> Clears the selection of the cards with Escape; the table clears its own. </summary>
        [RelayCommand]
        private void ClearSelection() => SelectedRows.Clear();

        /// <summary> Shows or hides the preview panel. </summary>
        [RelayCommand]
        private void TogglePreview() => IsPreviewOpen = !IsPreviewOpen;

        /// <summary> Opens the game of a double-clicked row or card. </summary>
        [RelayCommand]
        private void OpenGame(GameRow row) => GameSelected?.Invoke(row.Game);

        /// <summary> Opens the editor for the only selected game. </summary>
        [RelayCommand(CanExecute = nameof(HasSingleSelection))]
        private Task EditSelectedAsync() => EditAsync(SelectedRows[0].Game);

        private bool HasSingleSelection() => SelectedRows.Count == 1;

        private bool HasLibrarySelection() => !_isArchive && SelectedRows.Count > 0;

        private bool HasArchiveSelection() => _isArchive && SelectedRows.Count > 0;

        private bool HasAnySelection() => SelectedRows.Count > 0;

        /// <summary>
        /// Moves the selected games to the archive after one confirmation. Sessions still running in them
        /// would stop archiving, so they are named in the confirmation and ended in the same transaction.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasLibrarySelection))]
        private async Task ArchiveSelectedAsync()
        {
            var rows = SelectedRows.ToList();
            var ids = rows.Select(r => r.Game.GameId).ToList();

            List<PlaySession> running;
            try
            {
                running = SessionRepository.GetActive(ids);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка архівації ігор", ex);
                return;
            }

            var one = rows.Count == 1;
            var question = one
                ? $"Перенести «{rows[0].Title}» в архів?"
                : $"Перенести {Plural.Count(rows.Count, "гру", "гри", "ігор")} в архів?";
            var sessions = running.Count == 0
                ? string.Empty
                : one
                    ? $"\nУ грі триває сесія (з {running[0].StartTime:dd.MM.yyyy HH:mm}), її буде завершено зараз."
                    : "\nУ цих іграх триває сесія, її буде завершено зараз:\n" +
                      string.Join("\n", running.Select(r => $"• {r.GameTitle} (з {r.StartTime:dd.MM.yyyy HH:mm})"));
            var note = one
                ? "Гра зникне з бібліотеки та статистики, але її можна відновити в розділі «Архів»."
                : "Ігри зникнуть з бібліотеки та статистики, але їх можна відновити в розділі «Архів».";

            var confirmed = await Dialogs.Current.ConfirmAsync(
                one ? "Архівування гри" : "Архівування ігор",
                $"{question}{sessions}\n\n{note}",
                running.Count == 0 ? "В архів" : "Завершити й архівувати");
            if (!confirmed) return;

            try
            {
                GameRepository.Archive(ids);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка архівації ігор", ex);
                return;
            }
            Load();
        }

        /// <summary> Returns the selected games from the archive to the library after confirmation. </summary>
        [RelayCommand(CanExecute = nameof(HasArchiveSelection))]
        private async Task RestoreSelectedAsync()
        {
            var rows = SelectedRows.ToList();
            var one = rows.Count == 1;

            var confirmed = await Dialogs.Current.ConfirmAsync(
                one ? "Відновлення гри" : "Відновлення ігор",
                one
                    ? $"Повернути «{rows[0].Title}» в бібліотеку?\nГра знову з'явиться в бібліотеці та статистиці."
                    : $"Повернути {Plural.Count(rows.Count, "гру", "гри", "ігор")} в бібліотеку?\n" +
                      "Ігри знову з'являться в бібліотеці та статистиці.",
                "Відновити");
            if (!confirmed) return;

            try
            {
                GameRepository.Restore(rows.Select(r => r.Game.GameId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка відновлення ігор", ex);
                return;
            }
            Load();
        }

        /// <summary>
        /// Deletes the selected games after confirmation. Their sessions, goals, achievements and DLC
        /// are deleted by the database with them, and the confirmation says so.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasAnySelection))]
        private async Task DeleteSelectedAsync()
        {
            var rows = SelectedRows.ToList();
            var one = rows.Count == 1;

            var confirmed = await Dialogs.Current.ConfirmAsync(
                one ? "Видалення гри" : "Видалення ігор",
                one
                    ? $"Видалити «{rows[0].Title}»?\n" +
                      "Разом із грою буде видалено її сесії, цілі, досягнення та доповнення.\n" +
                      "Цю дію неможливо скасувати."
                    : $"Видалити {Plural.Count(rows.Count, "гру", "гри", "ігор")}?\n" +
                      "Разом з іграми буде видалено їхні сесії, цілі, досягнення та доповнення.\n" +
                      "Цю дію неможливо скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                GameRepository.Delete(rows.Select(r => r.Game.GameId).ToList());
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення ігор", ex);
                return;
            }
            Load();
        }

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}