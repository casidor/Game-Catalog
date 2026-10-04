using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the game detail page.
    /// </summary>
    public partial class GameDetailsViewModel : ViewModelBase
    {
        /// <summary>
        /// The game being displayed.
        /// </summary>
        public Game Game { get; }

        /// <summary>
        /// Raised when the user requests to go back to the previous page.
        /// </summary>
        public event Action? BackRequested;

        /// <summary>
        /// Indicates whether the game is in the archive.
        /// </summary>
        public bool IsArchived => Game.IsArchived;

        /// <summary> Game title. </summary>
        public string Title => Game.Title;

        /// <summary> Game genre. </summary>
        public string Genre => Game.Genre;

        /// <summary> Release year. </summary>
        public int ReleaseYear => Game.ReleaseYear;

        /// <summary> Gaming platform. </summary>
        public string Platform => Game.Platform;

        /// <summary> Disk size in GB. </summary>
        public double SizeGB => Game.SizeGB;

        /// <summary> Game status. </summary>
        public GameStatus Status => Game.Status;

        /// <summary> Hours played. </summary>
        public double HoursPlayed => Game.HoursPlayed;

        /// <summary> Personal rating. </summary>
        public int PersonalRating => Game.PersonalRating;

        /// <summary> Developer studio name. </summary>
        public string DeveloperName => Game.Developer?.Name ?? "Невідомо";

        /// <summary>Loaded bitmap of the game cover, or null if the cover is unavailable or corrupted.</summary>
        public Bitmap? CoverImage
        {
            get
            {
                if (!File.Exists(Game.CoverImagePath)) return null;
                try
                {
                    return new Bitmap(Game.CoverImagePath);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>Indicates whether a cover image is available for this game.</summary>
        public bool HasCover => File.Exists(Game.CoverImagePath);

        /// <summary>Plain-text description of the game.</summary>
        public string Description => Game.Description;

        /// <summary>Indicates whether a description is available for this game.</summary>
        public bool HasDescription => !string.IsNullOrWhiteSpace(Game.Description);

        /// <summary>Formatted disk size string, or "Невідомо" if size is not set.</summary>
        public string DisplaySizeGB => Game.SizeGB == 0 ? "Невідомо" : $"{Game.SizeGB} ГБ";

        /// <summary> Date and time the game was added to the library. </summary>
        public DateTime AddedAt => Game.AddedAt;

        /// <summary> Sessions of this game, newest first. </summary>
        public IEnumerable<PlaySession> GameSessions => AppData.Instance.Sessions
            .Where(s => s.GameId == Game.Id)
            .OrderByDescending(s => s.StartTime);

        /// <summary> Number of sessions of this game. </summary>
        public int SessionsCount => AppData.Instance.Sessions.Count(s => s.GameId == Game.Id);

        /// <summary> Indicates whether this game has any sessions. </summary>
        public bool HasSessions => SessionsCount > 0;

        /// <summary> Index of the selected page tab (0 = details, 1 = sessions). </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDetailsTab))]
        [NotifyPropertyChangedFor(nameof(IsSessionsTab))]
        private int _selectedTabIndex;

        /// <summary> True when the details tab is selected. </summary>
        public bool IsDetailsTab => SelectedTabIndex == 0;

        /// <summary> True when the sessions tab is selected. </summary>
        public bool IsSessionsTab => SelectedTabIndex == 1;

        /// <summary> Date of the most recent session, or a dash if there are none. </summary>
        public string LastPlayed
        {
            get
            {
                var last = AppData.Instance.Sessions
                    .Where(s => s.GameId == Game.Id)
                    .Select(s => (DateTime?)s.StartTime)
                    .Max();
                return last.HasValue ? last.Value.ToString("dd.MM.yyyy") : "—";
            }
        }

        /// <summary>Loaded bitmap of the game icon, or null if the icon is unavailable or corrupted.</summary>
        public Bitmap? IconImage
        {
            get
            {
                if (!File.Exists(Game.IconPath)) return null;
                try { return new Bitmap(Game.IconPath); }
                catch { return null; }
            }
        }

        /// <summary>Indicates whether an icon is available for this game.</summary>
        public bool HasIcon => File.Exists(Game.IconPath);

        public GameDetailsViewModel(Game game)
        {
            Game = game;
            AppData.Instance.Sessions.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HoursPlayed));
                OnPropertyChanged(nameof(GameSessions));
                OnPropertyChanged(nameof(SessionsCount));
                OnPropertyChanged(nameof(HasSessions));
                OnPropertyChanged(nameof(LastPlayed));
                OnPropertyChanged(nameof(IconImage));
                OnPropertyChanged(nameof(HasIcon));
            };
        }

        /// <summary>
        /// Refreshes all UI bindings after game data has been updated.
        /// </summary>
        public void RefreshGame()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Genre));
            OnPropertyChanged(nameof(ReleaseYear));
            OnPropertyChanged(nameof(Platform));
            OnPropertyChanged(nameof(SizeGB));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(HoursPlayed));
            OnPropertyChanged(nameof(PersonalRating));
            OnPropertyChanged(nameof(DeveloperName));
            OnPropertyChanged(nameof(CoverImage));
            OnPropertyChanged(nameof(HasCover));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(HasDescription));
            OnPropertyChanged(nameof(DisplaySizeGB));
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            BackRequested?.Invoke();
        }
    }
}
