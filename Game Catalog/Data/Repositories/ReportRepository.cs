using Dapper;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Data.Repositories
{
    /// <summary>
    /// Queries for the PDF reports.
    /// Every report covers standalone games (not DLC) that are not archived.
    /// </summary>
    public static class ReportRepository
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Returns the rows of the "Game library" report, ordered by title.
        /// Genres and platforms of a game are joined into one line each.
        /// </summary>
        public static List<LibraryReportRow> GetLibraryReport() =>
            Db.Run("Не вдалося завантажити дані звіту про бібліотеку", () =>
            {
                using var connection = Db.Open();
                // Genres and platforms are collected by correlated subqueries rather than two JOINs with GROUP BY:
                // two many-to-many JOINs would multiply the rows of a game (genres x platforms).
                // size_gb needs no check of the install folder: a CHECK keeps it NULL for games that are not installed.
                return connection.Query<LibraryReportRow>(@"
                    SELECT g.game_title, g.developer_name,
                           (SELECT group_concat(ge.genre_name, ', ' ORDER BY ge.genre_name COLLATE UKR_NOCASE)
                            FROM GameGenre gg
                            JOIN Genre ge ON ge.genre_id = gg.genre_id
                            WHERE gg.game_id = g.game_id) AS genres,
                           (SELECT group_concat(p.platform_name, ', ' ORDER BY p.platform_name COLLATE UKR_NOCASE)
                            FROM GamePlatform gp
                            JOIN Platform p ON p.platform_id = gp.platform_id
                            WHERE gp.game_id = g.game_id) AS platforms,
                           g.release_year, g.game_status, g.personal_rating, g.total_hours, g.size_gb
                    FROM v_game g
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                    ORDER BY g.game_title COLLATE UKR_NOCASE, g.game_id").AsList();
            });

        /// <summary>
        /// Returns the rows of the "Player goals" report, grouped by game.
        /// Within a game the goals that need attention come first: overdue, active, completed, cancelled,
        /// then by priority and the nearest due date.
        /// </summary>
        public static List<GoalReportRow> GetGoalReport() =>
            Db.Run("Не вдалося завантажити дані звіту про цілі", () =>
            {
                using var connection = Db.Open();
                // DLC need no filter here: a trigger does not let a DLC have goals.
                return connection.Query<GoalReportRow>(@"
                    SELECT game_id, game_title, goal_name, goal_description, priority,
                           created_at, due_date, display_status, completed_at, days_left
                    FROM v_goal
                    WHERE is_archived = 0
                    ORDER BY game_title COLLATE UKR_NOCASE, game_id,
                             CASE display_status
                                 WHEN 'Overdue'   THEN 0
                                 WHEN 'Active'    THEN 1
                                 WHEN 'Completed' THEN 2
                                 ELSE 3
                             END,
                             priority, due_date NULLS LAST, goal_id").AsList();
            });

        /// <summary>
        /// Returns the finished sessions started within the period, in chronological order,
        /// each with the totals of its day and of the whole period.
        /// </summary>
        /// <param name="from"> First day of the period, included. </param>
        /// <param name="to"> Last day of the period, included. </param>
        public static List<ActivityReportRow> GetActivityReport(DateTime from, DateTime to) =>
            Db.Run("Не вдалося завантажити дані звіту про активність", () =>
            {
                using var connection = Db.Open();
                // The period is a half-open interval [from, the day after to), as on the statistics page:
                // a session belongs to the day it started on, and the index on start_time can be used.
                // Window functions count the totals without collapsing the rows, unlike GROUP BY:
                // PARTITION BY the day gives the totals of the day of each session, an empty OVER () those of the period.
                return connection.Query<ActivityReportRow>(@"
                    SELECT g.game_title, s.start_time, s.duration_seconds, s.entry_method,
                           COUNT(*)                OVER (PARTITION BY date(s.start_time)) AS day_session_count,
                           SUM(s.duration_seconds) OVER (PARTITION BY date(s.start_time)) AS day_total_seconds,
                           COUNT(*)                OVER ()                                AS period_session_count,
                           SUM(s.duration_seconds) OVER ()                                AS period_total_seconds
                    FROM PlaySession s
                    JOIN Game g ON g.game_id = s.game_id
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                      AND s.duration_seconds IS NOT NULL
                      AND s.start_time >= @From
                      AND s.start_time <  @ToExclusive
                    ORDER BY s.start_time, s.play_session_id",
                    new
                    {
                        From = from.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
                        ToExclusive = to.Date.AddDays(1).ToString(DateFormat, CultureInfo.InvariantCulture)
                    }).AsList();
            });

        /// <summary> Returns the number of goals of games that are not archived, in total and by status. </summary>
        public static GoalSummary GetGoalSummary() =>
            Db.Run("Не вдалося завантажити кількість цілей", () =>
            {
                using var connection = Db.Open();
                // COUNT with FILTER gives 0 rather than NULL when there are no goals.
                return connection.QuerySingle<GoalSummary>(@"
                    SELECT COUNT(*)                                             AS total_count,
                           COUNT(*) FILTER (WHERE display_status = 'Active')    AS active_count,
                           COUNT(*) FILTER (WHERE display_status = 'Overdue')   AS overdue_count,
                           COUNT(*) FILTER (WHERE display_status = 'Completed') AS completed_count,
                           COUNT(*) FILTER (WHERE display_status = 'Cancelled') AS cancelled_count
                    FROM v_goal
                    WHERE is_archived = 0");
            });
    }
}
