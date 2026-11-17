using CommunityToolkit.Mvvm.ComponentModel;
using Game_Catalog.Models;

namespace Game_Catalog.ViewModels
{
    /// <summary> One row of the goals table: a goal plus the transient search-match flag. </summary>
    public partial class GoalRow : ObservableObject
    {
        /// <summary> The goal shown in this row. </summary>
        public Goal Goal { get; }

        /// <summary> Title of the game, exposed for column binding. </summary>
        public string GameTitle => Goal.GameTitle;

        /// <summary> Name of the goal, exposed for column binding. </summary>
        public string GoalName => Goal.GoalName;

        /// <summary> Description or null if there is none, exposed for column binding. </summary>
        public string? Description =>
            string.IsNullOrWhiteSpace(Goal.GoalDescription) ? null : Goal.GoalDescription;

        /// <summary> Priority from 1 (highest) to 5 (lowest), exposed for column binding. </summary>
        public int Priority => Goal.Priority;

        /// <summary> Status shown to the player, exposed for column binding. </summary>
        public GoalStatus Status => Goal.Status;

        /// <summary> Due date with the days left for an active goal, or null if there is no due date. </summary>
        public string? DueText => Goal.DueDate switch
        {
            null => null,
            { } due when Goal.Status == GoalStatus.Active && Goal.DaysLeft is { } left
                => $"{due:dd.MM.yyyy} ({left} дн.)",
            { } due => $"{due:dd.MM.yyyy}"
        };

        /// <summary> True when the due date has passed and the goal is still open; the date is shown in red. </summary>
        public bool IsOverdue => Goal.Status == GoalStatus.Overdue;

        /// <summary> Creation date and time, used as the sort path of the column. </summary>
        public System.DateTime CreatedAt => Goal.CreatedAt;

        /// <summary> Creation date shown in the table. </summary>
        public string CreatedText => $"{Goal.CreatedAt:dd.MM.yyyy}";

        /// <summary> Exact creation time shown in the tooltip. </summary>
        public string CreatedTip => $"{Goal.CreatedAt:dd.MM.yyyy HH:mm}";

        /// <summary> Completion date with the days it took, or null if the goal is not completed. </summary>
        public string? CompletedText => Goal.CompletedAt switch
        {
            null => null,
            { } done when Goal.CompletionDays is { } days => $"{done:dd.MM.yyyy} ({days} дн.)",
            { } done => $"{done:dd.MM.yyyy}"
        };

        /// <summary> Exact completion time shown in the tooltip, or null if the goal is not completed. </summary>
        public string? CompletedTip =>
            Goal.CompletedAt is { } done ? $"{done:dd.MM.yyyy HH:mm}" : null;

        /// <summary> True for completed and cancelled goals: their row is dimmed so open ones stand out. </summary>
        private bool IsClosed => Goal.Status is GoalStatus.Completed or GoalStatus.Cancelled;

        /// <summary> Opacity of the main cells of the row. </summary>
        public double TextOpacity => IsClosed ? 0.55 : 1;

        /// <summary> Opacity of the service dates (created, completed): muted in an open row, level with the rest in a closed one. </summary>
        public double ServiceOpacity => IsClosed ? 0.55 : 0.6;

        /// <summary> True when the row matches the current search text. </summary>
        [ObservableProperty] private bool _isMatch;

        public GoalRow(Goal goal) => Goal = goal;
    }
}