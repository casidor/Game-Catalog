using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;
using System;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// ViewModel for the statistics page.
    /// Computes aggregate metrics from the active game library.
    /// </summary>
    public partial class StatisticsViewModel : ViewModelBase
    {
        /// <summary>
        /// Dynamic disk capacity ceiling, rounded up to the nearest 500 GB above current usage.
        /// </summary>
        public double DiskMax => Math.Max(500, Math.Ceiling(TotalDiskGB / 500.0) * 500);

        /// <summary>
        /// Total number of games in the active library.
        /// </summary>
        public int TotalGames => AppData.Instance.Games.Count;

        /// <summary>
        /// Sum of hours played across all games in the library.
        /// </summary>
        public double TotalHours => AppData.Instance.Games.Sum(g => g.HoursPlayed);

        /// <summary>
        /// Sum of disk space occupied by all games in the library, in gigabytes.
        /// </summary>
        public double TotalDiskGB => AppData.Instance.Games.Sum(g => g.SizeGB);

        /// <summary>
        /// Average personal rating across all games, rounded to one decimal place.
        /// Returns zero when the library is empty.
        /// </summary>
        public double AverageRating => AppData.Instance.Games.Any()
            ? Math.Round(AppData.Instance.Games.Average(g => g.PersonalRating), 1)
            : 0;

        /// <summary>
        /// Average rating converted to a 1–5 star scale.
        /// </summary>
        public int StarRating => AppData.Instance.Games.Any()
            ? (int)Math.Round(AverageRating / 2.0)
            : 0;

        /// <summary>
        /// Unicode star string representing the current star rating (e.g. ★★★☆☆).
        /// </summary>
        public string StarDisplay =>
            new string('★', StarRating) + new string('☆', 5 - StarRating);

        /// <summary>
        /// Pixel width of the gold star overlay, proportional to the average rating.
        /// Used to simulate half-star precision via text clipping.
        /// </summary>
        public double StarFillWidth => AverageRating / 10.0 * 5 * 23;

        /// <summary>
        /// Initializes the view model and subscribes to library collection changes.
        /// </summary>
        public StatisticsViewModel()
        {
            AppData.Instance.Games.CollectionChanged += (_, _) => Refresh();
        }

        /// <summary>
        /// Raises property-changed notifications for all computed statistics.
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(TotalGames));
            OnPropertyChanged(nameof(TotalHours));
            OnPropertyChanged(nameof(TotalDiskGB));
            OnPropertyChanged(nameof(AverageRating));
            OnPropertyChanged(nameof(StarRating));
            OnPropertyChanged(nameof(StarDisplay));
            OnPropertyChanged(nameof(StarFillWidth));
            OnPropertyChanged(nameof(DiskMax));
        }
    }
}