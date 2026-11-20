using System;

namespace Game_Catalog.Models
{
    /// <summary> Row of the "activity for a period" statistic: play activity of one day. </summary>
    public class DailyActivity
    {
        /// <summary> The day. A session belongs to the day it started on. </summary>
        public DateTime ActivityDate { get; set; }

        /// <summary> Number of finished sessions started on that day. </summary>
        public int SessionCount { get; set; }

        /// <summary> Total duration of those sessions in seconds. </summary>
        public int TotalSeconds { get; set; }
    }
}
