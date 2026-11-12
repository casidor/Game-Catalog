using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Converters;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
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

        public ObservableCollection<FilterOption> GenreOptions { get; } = new();
        public ObservableCollection<FilterOption> PlatformOptions { get; } = new();
        public ObservableCollection<FilterOption> TagOptions { get; } = new();
        public ObservableCollection<FilterOption> DeveloperOptions { get; } = new();
        public ObservableCollection<FilterOption> StatusOptions { get; } = new();

        /// <summary> Text whose matches are highlighted. </summary>
        [ObservableProperty] private string _searchText = string.Empty;

        /// <summary> Field of the current sort order. </summary>
        [ObservableProperty] private GameSortField _sortField = GameSortField.Title;

        /// <summary> True for descending order. </summary>
        [ObservableProperty] private bool _sortDescending;

        /// <summary> True for the cover grid, false for the table. </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ViewModeText))]
        [NotifyPropertyChangedFor(nameof(ShowGrid))]
        [NotifyPropertyChangedFor(nameof(ShowList))]
        private bool _isGridView = true;

        [ObservableProperty] private bool _isFilterPanelOpen;

        [ObservableProperty] private int? _draftYearFrom;
        [ObservableProperty] private int? _draftYearTo;
        [ObservableProperty] private int? _draftRatingFrom;
        [ObservableProperty] private int? _draftRatingTo;
        [ObservableProperty] private double? _draftSizeFrom;
        [ObservableProperty] private double? _draftSizeTo;
        [ObservableProperty] private double? _draftHoursFrom;
        [ObservableProperty] private double? _draftHoursTo;

        /// <summary> Caption of the view mode button: the mode shown right now. </summary>
        public string ViewModeText => IsGridView ? "▦ Сітка" : "☰ Список";

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
                return;
            }

            SortField = field;
            SortDescending = false;
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
        }

        partial void OnSearchTextChanged(string value) => UpdateMatches();
        partial void OnSortFieldChanged(GameSortField value) => Rebuild();
        partial void OnSortDescendingChanged(bool value) => Rebuild();

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

        /// <summary> Loads games matching the applied filter from the database. </summary>
        private void Reload()
        {
            try
            {
                _loaded = GameRepository.GetAll(_applied with { IsArchived = _isArchive });
            }
            catch (DatabaseException ex)
            {
                _loaded = new List<Game>();
                _ = ShowErrorAsync("Помилка завантаження ігор", ex);
            }

            Rebuild();
        }

        /// <summary> Sorts the loaded games in memory and rebuilds the rows. </summary>
        private void Rebuild()
        {
            Rows.Clear();
            foreach (var game in Sort(_loaded))
                Rows.Add(new GameRow(game));

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

        /// <summary> Orders games by the chosen field. Games without a value always go last. </summary>
        private List<Game> Sort(List<Game> source)
        {
            var comparer = StringComparer.CurrentCultureIgnoreCase;

            if (SortField == GameSortField.Title)
            {
                var byTitle = SortDescending
                    ? source.OrderByDescending(g => g.GameTitle, comparer)
                    : source.OrderBy(g => g.GameTitle, comparer);
                return byTitle.ThenBy(g => g.GameId).ToList();
            }

            Func<Game, double?> key = SortField switch
            {
                GameSortField.Year => g => g.ReleaseYear,
                GameSortField.Rating => g => g.PersonalRating,
                GameSortField.Size => g => g.SizeGB,
                _ => g => g.TotalHours
            };

            var withValue = source.Where(g => key(g) != null);
            var withoutValue = source.Where(g => key(g) == null);

            var sorted = SortDescending
                ? withValue.OrderByDescending(g => key(g))
                : withValue.OrderBy(g => key(g));

            return sorted.ThenBy(g => g.GameTitle, comparer)
                .Concat(withoutValue.OrderBy(g => g.GameTitle, comparer))
                .ToList();
        }

        /// <summary> Marks rows whose title or studio name contains the search text. </summary>
        private void UpdateMatches()
        {
            var query = SearchText.Trim();
            foreach (var row in Rows)
            {
                row.IsMatch = query.Length > 0 &&
                    (row.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     row.DeveloperName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
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
        private void SetSortField(GameSortField field) => SortField = field;

        /// <summary> Sets ascending order. </summary>
        [RelayCommand]
        private void SetAscending() => SortDescending = false;

        /// <summary> Sets descending order. </summary>
        [RelayCommand]
        private void SetDescending() => SortDescending = true;

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
        /// <summary> Opens the game. The details page arrives in a later commit. </summary>
        [RelayCommand]
        private void OpenGame(Game game) => GameSelected?.Invoke(game);

        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}