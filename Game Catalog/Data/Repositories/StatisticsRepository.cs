using Dapper;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Data.Repositories
{
    /// <summary>
    /// Summary queries for the statistics page.
    /// Every statistic covers standalone games (not DLC) that are not archived.
    /// </summary>
    public static class StatisticsRepository
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

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

        /// <summary>
        /// Returns studios that have games in the library with the number of games,
        /// total play time and average rating, from the most played studio to the least.
        /// </summary>
        public static List<StudioStat> GetStudioStats() =>
            Db.Run("Не вдалося завантажити статистику за студіями", () =>
            {
                using var connection = Db.Open();
                // v_game has one row per game with its play time already summed, so the join
                // gives one row per game of the studio: COUNT(*) counts games and AVG is not weighted by sessions.
                // The inner join leaves out studios without games; WHERE filters games before grouping.
                return connection.Query<StudioStat>(@"
                    SELECT s.studio_id, s.studio_name,
                           COUNT(*)               AS game_count,
                           SUM(g.total_hours)     AS total_hours,
                           AVG(g.personal_rating) AS average_rating
                    FROM Studio s
                    JOIN v_game g ON g.developer_id = s.studio_id
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                    GROUP BY s.studio_id, s.studio_name
                    ORDER BY total_hours DESC,
                             game_count DESC,
                             s.studio_name COLLATE UKR_NOCASE ASC,
                             s.studio_id ASC").AsList();
            });

        /// <summary>
        /// Returns genres that have games in the library with the number of games and total play time,
        /// from the genre with the most games to the least. A game with several genres counts in each of them.
        /// </summary>
        public static List<GenreStat> GetGenreStats() =>
            Db.Run("Не вдалося завантажити статистику за жанрами", () =>
            {
                using var connection = Db.Open();
                // The link table gives one row per genre and game pair (its primary key keeps pairs unique),
                // so COUNT(*) counts games without DISTINCT. Play time comes from v_game, already summed per game.
                return connection.Query<GenreStat>(@"
                    SELECT ge.genre_id, ge.genre_name,
                           COUNT(*)           AS game_count,
                           SUM(g.total_hours) AS total_hours
                    FROM Genre ge
                    JOIN GameGenre gg ON gg.genre_id = ge.genre_id
                    JOIN v_game g     ON g.game_id   = gg.game_id
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                    GROUP BY ge.genre_id, ge.genre_name
                    ORDER BY game_count DESC,
                             total_hours DESC,
                             ge.genre_name COLLATE UKR_NOCASE ASC,
                             ge.genre_id ASC").AsList();
            });

        /// <summary>
        /// Returns play activity by day for the period from <paramref name="from"/> to <paramref name="to"/>, both days included,
        /// newest day first. Only finished sessions count; a session belongs to the day it started on; days without sessions are left out.
        /// </summary>
        public static List<DailyActivity> GetDailyActivity(DateTime from, DateTime to) =>
            Db.Run("Не вдалося завантажити активність за період", () =>
            {
                using var connection = Db.Open();
                // The period is a half-open interval [from, the day after to): start_time is compared as it is,
                // so the index on it can be used, and the whole last day is included.
                // Text dates in the yyyy-MM-dd HH:mm:ss format compare in chronological order.
                return connection.Query<DailyActivity>(@"
                    SELECT date(s.start_time)      AS activity_date,
                           COUNT(*)                AS session_count,
                           SUM(s.duration_seconds) AS total_seconds
                    FROM PlaySession s
                    JOIN Game g ON g.game_id = s.game_id
                    WHERE g.is_archived = 0
                      AND g.parent_game_id IS NULL
                      AND s.duration_seconds IS NOT NULL
                      AND s.start_time >= @From
                      AND s.start_time <  @ToExclusive
                    GROUP BY date(s.start_time)
                    ORDER BY activity_date DESC",
                    new
                    {
                        From = from.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
                        ToExclusive = to.Date.AddDays(1).ToString(DateFormat, CultureInfo.InvariantCulture)
                    }).AsList();
            });
    }
}
