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
    }
}
