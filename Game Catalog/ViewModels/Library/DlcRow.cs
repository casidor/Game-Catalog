using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of the DLC tab: a DLC plus the texts of its row and of the details shown under it. </summary>
    public class DlcRow
    {
        /// <summary> The DLC shown in this row. </summary>
        public Game Dlc { get; }

        public string Title => Dlc.GameTitle;
        // Texts below are null when the DLC has no value; the view decides how to show that.
        public string? YearText => Dlc.ReleaseYear?.ToString();
        public GameStatus Status => Dlc.GameStatus;
        public string? RatingText => Dlc.PersonalRating is { } r ? $"{r}/10" : null;

        // Details under the selected row.
        public string DeveloperName => Dlc.DeveloperName;
        public string AddedText => $"Додано {Dlc.AddedAt:dd.MM.yyyy}";
        public string CoverImagePath => Dlc.CoverImagePath;
        public string Description => Dlc.GameDescription;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Dlc.GameDescription);

        public DlcRow(Game dlc) => Dlc = dlc;
    }
}
