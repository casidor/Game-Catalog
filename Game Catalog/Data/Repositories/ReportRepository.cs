using Dapper;
using Game_Catalog.Models;
using System.Collections.Generic;

namespace Game_Catalog.Data.Repositories
{
    /// <summary>
    /// Queries for the PDF reports.
    /// Every report covers standalone games (not DLC) that are not archived.
    /// </summary>
    public static class ReportRepository
    {
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
