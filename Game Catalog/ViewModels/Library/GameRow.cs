using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of the games list: a game plus display texts, the search-match flag and the selection flag. </summary>
    public partial class GameRow : ObservableObject
    {
        /// <summary> The game shown in this row. </summary>
        public Game Game { get; }

        public string Title => Game.GameTitle;
        public string DeveloperName => Game.DeveloperName;
        // Texts below are null when the game has no value; the view decides how to show that.
        public string? PlatformsText => Game.Platforms.Count == 0
            ? null
            : string.Join(", ", Game.Platforms.Select(p => p.PlatformName));
        public string? GenresText => Game.Genres.Count == 0
            ? null
            : string.Join(", ", Game.Genres.Select(g => g.GenreName));
        public string? YearText => Game.ReleaseYear?.ToString();
        public GameStatus Status => Game.GameStatus;
        public string? RatingText => Game.PersonalRating is { } r ? $"{r}/10" : null;
        public double TotalHours => Game.TotalHours;
        public string? SizeText => Game.SizeGB is { } s ? $"{s:0.0} ГБ" : null;
        public string CoverImagePath => Game.CoverImagePath;

        /// <summary> Platforms shown as badges in the preview panel. </summary>
        public IReadOnlyList<Platform> Platforms => Game.Platforms;
        public bool HasPlatforms => Game.Platforms.Count > 0;

        /// <summary> Tags shown as colored badges in the preview panel. </summary>
        public IReadOnlyList<Tag> Tags => Game.Tags;
        public bool HasTags => Game.Tags.Count > 0;

        /// <summary> True when the row matches the current search text. </summary>
        [ObservableProperty] private bool _isMatch;

        /// <summary> True when the row is selected and shown in the preview panel. </summary>
        [ObservableProperty] private bool _isSelected;

        public GameRow(Game game) => Game = game;
    }
}