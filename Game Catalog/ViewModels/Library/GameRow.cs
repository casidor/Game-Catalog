using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of the games list: a game plus display texts and the search-match flag. </summary>
    public partial class GameRow : ObservableObject
    {
        /// <summary> The game shown in this row. </summary>
        public Game Game { get; }

        public string Title => Game.GameTitle;
        public string DeveloperName => Game.DeveloperName;
        public string PlatformsText => string.Join(", ", Game.Platforms.Select(p => p.PlatformName));
        public string YearText => Game.ReleaseYear?.ToString() ?? "—";
        public GameStatus Status => Game.GameStatus;
        public string RatingText => Game.PersonalRating is { } r ? $"{r}/10" : "—";
        public double TotalHours => Game.TotalHours;
        public string SizeText => Game.SizeGB is { } s ? $"{s:0.0} ГБ" : "Невідомо";
        public string CoverImagePath => Game.CoverImagePath;

        /// <summary> True when the row matches the current search text. </summary>
        [ObservableProperty] private bool _isMatch;

        public GameRow(Game game) => Game = game;
    }
}