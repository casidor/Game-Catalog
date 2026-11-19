using Dapper;
using Game_Catalog.Models;

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
    }
}
