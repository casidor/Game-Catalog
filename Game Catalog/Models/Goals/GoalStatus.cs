namespace Game_Catalog.Models
{
    /// <summary>
    /// Status of a goal as shown to the player. Read from the database, never written by the model:
    /// the table stores only Active, Completed and Cancelled, Overdue is computed by the v_goal view.
    /// </summary>
    public enum GoalStatus
    {
        /// <summary> In progress; the due date is not set or has not passed. </summary>
        Active,

        /// <summary> Marked as completed by the player. </summary>
        Completed,

        /// <summary> Cancelled by the player. </summary>
        Cancelled,

        /// <summary> Still active, but the due date has passed. </summary>
        Overdue
    }
}