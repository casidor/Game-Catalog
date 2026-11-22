using System;

namespace Game_Catalog.Models
{
    /// <summary>
    /// Row of the "Activity for a period" report: a finished session of a game that is not archived.
    /// Every row also carries the totals of its day and of the whole period, counted by the database.
    /// </summary>
    public class ActivityReportRow
    {
        /// <summary> Title of the game. </summary>
        public string GameTitle { get; set; } = string.Empty;

        /// <summary> When the session started. The session belongs to the day it started on. </summary>
        public DateTime StartTime { get; set; }

        /// <summary> Length of the session in seconds. </summary>
        public int DurationSeconds { get; set; }

        /// <summary> How the session was added. </summary>
        public EntryMethod EntryMethod { get; set; }

        /// <summary> Number of sessions on the day of this session. </summary>
        public int DaySessionCount { get; set; }

        /// <summary> Total length of the sessions on the day of this session, in seconds. </summary>
        public int DayTotalSeconds { get; set; }

        /// <summary> Number of sessions in the whole period. </summary>
        public int PeriodSessionCount { get; set; }

        /// <summary> Total length of the sessions in the whole period, in seconds. </summary>
        public int PeriodTotalSeconds { get; set; }
    }
}
