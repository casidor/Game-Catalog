using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Data.Repositories;
using Game_Catalog.Models;
using Game_Catalog.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DatabaseException = Game_Catalog.Data.DatabaseException;

namespace Game_Catalog.ViewModels
{
    /// <summary> ViewModel for the studio details page: the studio and its games and DLC. </summary>
    public partial class StudioDetailsViewModel : ViewModelBase
    {
        /// <summary> The studio being displayed. </summary>
        public Studio Studio { get; private set; }

        /// <summary> Raised when the user requests to go back to the previous page. </summary>
        public event Action? BackRequested;

        /// <summary> Raised with the id of the game to open; the main window opens its details page. </summary>
        public event Action<int>? GameSelected;

        /// <summary> Rows of the games table: games and DLC of other studios' games; other DLC are inside their game rows. </summary>
        public ObservableCollection<StudioGameRow> Rows { get; } = new();

        /// <summary> Text above the table, e.g. "4 гри, з них 1 доповнення і 1 в архіві". </summary>
        [ObservableProperty] private string _gamesSummary = string.Empty;

        /// <summary>
        /// True when at least one game in the table has an icon. Only then the rows keep a slot for it,
        /// so a table without icons has no empty gap before the titles.
        /// </summary>
        [ObservableProperty] private bool _showIcons;

        /// <summary> True when the studio has no games at all. </summary>
        public bool IsGamesEmpty => Rows.Count == 0;

        /// <summary> Name of the studio. </summary>
        public string Name => Studio.StudioName;

        /// <summary> Country, foundation year and main genre joined in one line; missing ones are left out. Null when none is set. </summary>
        public string? FactsText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Studio.Country)) parts.Add(Studio.Country);
                if (Studio.FoundationYear is { } year) parts.Add($"заснована у {year}");
                if (!string.IsNullOrWhiteSpace(Studio.MainGenreName)) parts.Add($"основний жанр: {Studio.MainGenreName}");
                return parts.Count > 0 ? string.Join(" · ", parts) : null;
            }
        }

        public bool HasFacts => FactsText != null;

        /// <summary> Website URL or null if not specified. </summary>
        public string? DisplayWebsite =>
            string.IsNullOrWhiteSpace(Studio.Website) ? null : Studio.Website;

        /// <summary>
        /// Address of the website for the link, with "https://" added when the scheme is missing.
        /// Null when there is no website or it is not a valid address.
        /// </summary>
        public Uri? WebsiteUri
        {
            get
            {
                var url = Studio.Website.Trim();
                if (url.Length == 0) return null;

                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    url = "https://" + url;

                return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
            }
        }

        /// <summary> True when the website can be opened as a link. </summary>
        public bool HasWebsite => WebsiteUri != null;

        /// <summary> True when the facts and the link both show, so a dot separates them. </summary>
        public bool ShowWebsiteSeparator => HasFacts && HasWebsite;

        /// <summary> True when the line under the name has anything to show. </summary>
        public bool HasSubtitle => HasFacts || HasWebsite;

        public StudioDetailsViewModel(Studio studio)
        {
            Studio = studio;
        }

        /// <summary> Loads the studio and its games each time the page is shown, also when coming back from a game. </summary>
        public override void OnNavigatedTo() => _ = ReloadAsync();

        /// <summary> Raises change notifications for everything shown about the studio. </summary>
        private void RefreshAll()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FactsText));
            OnPropertyChanged(nameof(HasFacts));
            OnPropertyChanged(nameof(DisplayWebsite));
            OnPropertyChanged(nameof(WebsiteUri));
            OnPropertyChanged(nameof(HasWebsite));
            OnPropertyChanged(nameof(ShowWebsiteSeparator));
            OnPropertyChanged(nameof(HasSubtitle));
        }

        /// <summary> Reloads the studio and its games from the database. Goes back if the studio no longer exists. </summary>
        private async Task ReloadAsync()
        {
            Studio? fresh;
            try
            {
                fresh = StudioRepository.GetById(Studio.StudioId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка завантаження студії", ex);
                return;
            }

            if (fresh == null)
            {
                BackRequested?.Invoke();
                return;
            }

            Studio = fresh;
            RefreshAll();
            await LoadGamesAsync();
        }

        /// <summary>
        /// Loads the games of the studio and arranges them for the table: a DLC of a game of this studio goes
        /// under that game, any other game or DLC gets its own row. The order is kept as the database returned it.
        /// </summary>
        private async Task LoadGamesAsync()
        {
            List<StudioGame> games;
            try
            {
                games = StudioRepository.GetGames(Studio.StudioId);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка завантаження ігор студії", ex);
                return;
            }

            var rows = games.Select(g => new StudioGameRow(g)).ToList();
            var topRows = rows.Where(r => !r.Game.IsNested).ToDictionary(r => r.Game.GameId);
            foreach (var dlc in rows.Where(r => r.Game.IsNested))
                topRows[dlc.Game.ParentGameId!.Value].Dlcs.Add(dlc);

            Rows.Clear();
            foreach (var row in rows.Where(r => !r.Game.IsNested))
                Rows.Add(row);

            ShowIcons = Rows.Any(r => r.IconPath != null);
            GamesSummary = SummaryOf(games);
            OnPropertyChanged(nameof(IsGamesEmpty));
        }

        /// <summary> Counts the loaded games for the text above the table. </summary>
        private static string SummaryOf(List<StudioGame> games)
        {
            var dlc = games.Count(g => g.ParentGameId != null);
            var archived = games.Count(g => g.IsArchived);

            var parts = new List<string>();
            if (dlc > 0) parts.Add(Plural.Count(dlc, "доповнення", "доповнення", "доповнень"));
            if (archived > 0) parts.Add($"{archived} в архіві");

            var total = Plural.Count(games.Count, "гра", "гри", "ігор");
            return parts.Count > 0 ? $"{total}, з них {string.Join(" і ", parts)}" : total;
        }

        /// <summary> Opens the game of a double-clicked row; a DLC opens its main game. </summary>
        [RelayCommand]
        private void OpenGame(StudioGameRow row) => GameSelected?.Invoke(row.GameIdToOpen);

        /// <summary> Opens the editor and saves the changes. </summary>
        [RelayCommand]
        private async Task EditAsync()
        {
            var edited = await Dialogs.Current.ShowStudioEditorAsync(Studio);
            if (edited == null) return;

            try
            {
                StudioRepository.Update(edited);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка збереження студії", ex);
                return;
            }

            await ReloadAsync();
        }

        /// <summary>
        /// Deletes the studio after confirmation. A studio that still has games cannot be deleted,
        /// so that is checked first and explained instead of asking.
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            var ids = new[] { Studio.StudioId };

            bool hasGames;
            try
            {
                hasGames = StudioRepository.GetIdsWithGames(ids).Count > 0;
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студії", ex);
                return;
            }

            if (hasGames)
            {
                await Dialogs.Current.AlertAsync(
                    "Неможливо видалити студію",
                    $"Студія «{Studio.StudioName}» має ігри в бібліотеці або архіві.\n" +
                    "Спочатку видаліть ці ігри або перепризначте їх іншій студії.");
                return;
            }

            var confirmed = await Dialogs.Current.ConfirmAsync(
                "Видалення студії",
                $"Видалити студію «{Studio.StudioName}»?\nЦю дію неможливо скасувати.",
                "Видалити");
            if (!confirmed) return;

            try
            {
                StudioRepository.Delete(ids);
            }
            catch (DatabaseException ex)
            {
                await ShowErrorAsync("Помилка видалення студії", ex);
                return;
            }

            BackRequested?.Invoke();
        }

        /// <summary> Navigates back to the previous page. </summary>
        [RelayCommand]
        private void GoBack() => BackRequested?.Invoke();

        /// <summary> Shows a plain alert for our own errors and a detailed one for database failures. </summary>
        private static Task ShowErrorAsync(string title, DatabaseException ex) =>
            ex.InnerException == null
                ? Dialogs.Current.AlertAsync(title, ex.Message)
                : Dialogs.Current.ErrorAsync(title, ex.Message, ex);
    }
}
