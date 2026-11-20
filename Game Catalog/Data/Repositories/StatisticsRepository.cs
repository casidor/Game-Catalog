using Dapper;
using Game_Catalog.Models;
using System.Collections.Generic;

namespace Game_Catalog.Data.Repositories
{
    /// <summary>
    /// Summary queries for the statistics page.
    /// Every statistic covers standalone games (not DLC) that are not archived.
    /// </summary>
    public static class StatisticsRepository
    {
        /// <summary>
        /// Returns the overall state of the library: game count, total play time,
        /// total size of installed games and the average rating.
        /// </summary>
        public static LibrarySummary GetLibrarySummary() =>
            Db.Run("Не вдалося завантажити статистику бібліотеки", () =>
            {
                using var connection = Db.Open();
                // total_hours in v_game already sums finished sessions per game.
                // SUM and AVG skip NULLs: the size is NULL when nothing is measured, the rating when nothing is rated.
                return connection.QuerySingle<LibrarySummary>(@"
                    SELECT COUNT(*)                      AS game_count,
                           COALESCE(SUM(total_hours), 0) AS total_hours,
                           SUM(size_gb)                  AS total_size_gb,
                           AVG(personal_rating)          AS average_rating
                    FROM v_game
                    WHERE is_archived = 0
                      AND parent_game_id IS NULL");
            });

        /// <summary>
        /// Returns games with finished sessions, from the most played to the least,
        /// with the share of each game in the total play time of the library.
        /// </summary>
        public static List<GamePlayTime> GetGamesByPlayTime() =>
            Db.Run("Не вдалося завантажити статистику часу гри", () =>
            {
                using var connection = Db.Open();
                // SUM(...) OVER () adds the total of all result rows to every row without grouping them,
                // so the share needs no separate subquery. The total matches the overview play time.
                return connection.Query<GamePlayTime>(@"
                    SELECT game_id, game_title, total_hours,
                           100.0 * total_hours / SUM(total_hours) OVER () AS share_percent
                    FROM v_game
                    WHERE is_archived = 0
                      AND parent_game_id IS NULL
                      AND total_hours > 0
                    ORDER BY total_hours DESC,
                             game_title COLLATE UKR_NOCASE ASC,
                             game_id ASC").AsList();
            });

        /// <summary>
        /// Returns the longest finished session, or null when there is none.
        /// Ties are broken by the earlier start, then by the game title;
        /// the session id is the last key, so the result is always the same.
        /// </summary>
        public static LongestSession? GetLongestSession() =>
            Db.Run("Не вдалося завантажити найдовшу сесію", () =>
            {
                using var connection = Db.Open();
                return connection.QuerySingleOrDefault<LongestSession>(@"
                    SELECT s.game_id, g.game_title, g.cover_image_path, s.start_time, s.duration_seconds
                    FROM PlaySession s
                    JOIN Game g ON g.game_id = s.game_id
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                      AND s.duration_seconds IS NOT NULL
                    ORDER BY s.duration_seconds DESC,
                             s.start_time ASC,
                             g.game_title COLLATE UKR_NOCASE ASC,
                             s.play_session_id ASC
                    LIMIT 1");
            });
    }
}
