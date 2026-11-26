using Game_Catalog.Models;
using System.Collections.Generic;

namespace Game_Catalog.ViewModels
{
    /// <summary>
    /// One row of the games table on the studio details page: a game with the DLC of the same studio listed under it,
    /// or a DLC whose main game belongs to another studio.
    /// </summary>
    public class StudioGameRow
    {
        public StudioGame Game { get; }

        /// <summary> DLC of this studio for the game, shown under its row when the row is selected. </summary>
        public List<StudioGameRow> Dlcs { get; } = new();

        public string Title => Game.GameTitle;
        // Texts below are null when the game has no value; the view decides how to show that.
        public string? YearText => Game.ReleaseYear?.ToString();
        public GameStatus Status => Game.GameStatus;
        public string? RatingText => Game.PersonalRating is { } r ? $"{r}/10" : null;
        public string? IconPath => Game.IconPath;

        /// <summary> True when the game is archived; for a DLC, when its main game is archived. </summary>
        public bool IsArchived => Game.IsArchived;

        public bool HasDlcs => Dlcs.Count > 0;
        public string DlcBadgeText => $"+{Dlcs.Count} DLC";

        /// <summary> True for a DLC shown as its own row, since its main game belongs to another studio. </summary>
        public bool IsDlc => Game.ParentGameId != null;
        public string? ParentText => IsDlc ? $"доповнення до «{Game.ParentTitle}»" : null;

        /// <summary> Game to open from this row: the main game for a DLC, which has no page of its own. </summary>
        public int GameIdToOpen => Game.ParentGameId ?? Game.GameId;

        public StudioGameRow(StudioGame game) => Game = game;
    }
}
