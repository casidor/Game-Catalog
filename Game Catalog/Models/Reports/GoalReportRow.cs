using System;

namespace Game_Catalog.Models
{
    /// <summary> Row of the "Player goals" report: a goal of a game that is not archived. </summary>
    public class GoalReportRow
    {
        /// <summary> Identifier of the game, used to group the goals of one game. </summary>
        public int GameId { get; set; }

        /// <summary> Title of the game. </summary>
        public string GameTitle { get; set; } = string.Empty;

        /// <summary> Name of the goal. </summary>
        public string GoalName { get; set; } = string.Empty;

        /// <summary> Description of the goal, or null when there is none. </summary>
        public string? GoalDescription { get; set; }

        /// <summary> Priority from 1 (the highest) to 5 (the lowest). </summary>
        public int Priority { get; set; }

        /// <summary> When the goal was created. </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary> Planned due date, or null when the goal has no deadline. </summary>
        public DateTime? DueDate { get; set; }

        /// <summary> Status as shown to the player: an active goal past its due date is overdue. </summary>
        public GoalStatus DisplayStatus { get; set; }

        /// <summary> When the goal was marked completed; null for other statuses. </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary> Days left until the due date for an active goal with a deadline, otherwise null. </summary>
        public int? DaysLeft { get; set; }
    }
}
