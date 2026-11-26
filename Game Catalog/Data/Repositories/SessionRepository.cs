using Dapper;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for play sessions (read through the v_play_session view). </summary>
    public static class SessionRepository
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Returns sessions matching the filter in the order chosen in it (newest first by default).
        /// Filtering and sorting are done by the database; sessions still in progress have no duration and go last.
        /// </summary>
        public static List<PlaySession> GetAll(SessionFilter filter)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (filter.SortField, filter.SortDescending) switch
            {
                (SessionSortField.Duration, false) =>
                    "duration_seconds ASC NULLS LAST, start_time DESC, play_session_id DESC",
                (SessionSortField.Duration, true) =>
                    "duration_seconds DESC NULLS LAST, start_time DESC, play_session_id DESC",
                (SessionSortField.Game, false) =>
                    "game_title COLLATE UKR_NOCASE ASC, start_time DESC, play_session_id DESC",
                (SessionSortField.Game, true) =>
                    "game_title COLLATE UKR_NOCASE DESC, start_time DESC, play_session_id DESC",
                (_, false) => "start_time ASC, play_session_id ASC",
                _ => "start_time DESC, play_session_id DESC"
            };

            return Db.Run("Не вдалося завантажити сесії", () =>
            {
                using var connection = Db.Open();
                return connection.Query<PlaySession>($@"
                    SELECT play_session_id, game_id, game_title, start_time, end_time,
                           duration_seconds, entry_method, note
                    FROM v_play_session
                    WHERE is_archived = 0
                      AND (@GameCount = 0 OR game_id IN @GameIds)
                      AND (@DateFrom IS NULL OR start_time >= @DateFrom)
                      AND (@DateToExclusive IS NULL OR start_time < @DateToExclusive)
                      AND (@DurationFrom IS NULL OR duration_seconds >= @DurationFrom)
                      AND (@DurationTo IS NULL OR duration_seconds <= @DurationTo)
                      AND (@Method IS NULL OR entry_method = @Method)
                    ORDER BY {orderBy}",
                    ToFilterParams(filter)).AsList();
            });
        }

        /// <summary>
        /// Returns all sessions of one game in the chosen order (newest first by default), for the sessions tab
        /// of the game details. Sessions still in progress have no duration and go last when sorted by it.
        /// </summary>
        public static List<PlaySession> GetByGame(int gameId, SessionSortField sortField, bool sortDescending)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (sortField, sortDescending) switch
            {
                (SessionSortField.Duration, false) =>
                    "duration_seconds ASC NULLS LAST, start_time DESC, play_session_id DESC",
                (SessionSortField.Duration, true) =>
                    "duration_seconds DESC NULLS LAST, start_time DESC, play_session_id DESC",
                (_, false) => "start_time ASC, play_session_id ASC",
                _ => "start_time DESC, play_session_id DESC"
            };

            return Db.Run("Не вдалося завантажити сесії гри", () =>
            {
                using var connection = Db.Open();
                return connection.Query<PlaySession>($@"
                    SELECT play_session_id, game_id, game_title, start_time, end_time,
                           duration_seconds, entry_method, note
                    FROM v_play_session
                    WHERE game_id = @gameId
                    ORDER BY {orderBy}",
                    new { gameId }).AsList();
            });
        }

        /// <summary> Returns the open session of the game, or null if the game is not being played. </summary>
        public static PlaySession? GetActive(int gameId) =>
            Db.Run("Не вдалося завантажити поточну сесію", () =>
            {
                using var connection = Db.Open();
                return connection.QuerySingleOrDefault<PlaySession>(@"
                    SELECT play_session_id, game_id, game_title, start_time, end_time,
                           duration_seconds, entry_method, note
                    FROM v_play_session
                    WHERE game_id = @gameId AND end_time IS NULL",
                    new { gameId });
            });

        /// <summary> Returns the sessions still running in the given games, oldest first. </summary>
        public static List<PlaySession> GetActive(IReadOnlyCollection<int> gameIds) =>
            Db.Run("Не вдалося завантажити поточні сесії", () =>
            {
                using var connection = Db.Open();
                return connection.Query<PlaySession>(@"
                    SELECT play_session_id, game_id, game_title, start_time, end_time,
                           duration_seconds, entry_method, note
                    FROM v_play_session
                    WHERE game_id IN @gameIds AND end_time IS NULL
                    ORDER BY start_time",
                    new { gameIds }).AsList();
            });

        /// <summary> Returns the open sessions of all games, oldest first. </summary>
        public static List<PlaySession> GetAllActive() =>
            Db.Run("Не вдалося завантажити поточні сесії", () =>
            {
                using var connection = Db.Open();
                return connection.Query<PlaySession>(@"
                    SELECT play_session_id, game_id, game_title, start_time, end_time,
                           duration_seconds, entry_method, note
                    FROM v_play_session
                    WHERE end_time IS NULL
                    ORDER BY start_time ASC, play_session_id ASC").AsList();
            });

        /// <summary> Ends all open sessions now in one statement. </summary>
        public static void StopAll() =>
            Db.Run("Не вдалося завершити сесії", () =>
            {
                using var connection = Db.Open();
                connection.Execute(@"
                    UPDATE PlaySession
                    SET end_time = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                    WHERE end_time IS NULL");
            });

        /// <summary> Inserts the session and sets its generated id. </summary>
        public static void Insert(PlaySession session) =>
            Db.Run("Не вдалося зберегти сесію", () =>
            {
                using var connection = Db.Open();
                session.PlaySessionId = connection.ExecuteScalar<int>(@"
                    INSERT INTO PlaySession (game_id, start_time, end_time, entry_method, note)
                    VALUES (@GameId, @StartTime, @EndTime, @EntryMethod, NULLIF(trim(@Note), ''));
                    SELECT last_insert_rowid();", ToParams(session));
            });

        /// <summary> Updates the game, times, method and note of the session. </summary>
        public static void Update(PlaySession session) =>
            Db.Run("Не вдалося оновити сесію", () =>
            {
                using var connection = Db.Open();
                connection.Execute(@"
                    UPDATE PlaySession
                    SET game_id = @GameId,
                        start_time = @StartTime,
                        end_time = @EndTime,
                        entry_method = @EntryMethod,
                        note = NULLIF(trim(@Note), '')
                    WHERE play_session_id = @PlaySessionId", ToParams(session));
            });

        /// <summary> Deletes the sessions, all or none. </summary>
        public static void Delete(IReadOnlyCollection<int> playSessionIds) =>
            Db.InTransaction("Не вдалося видалити сесії", (connection, tx) =>
            {
                foreach (var playSessionId in playSessionIds)
                    connection.Execute("DELETE FROM PlaySession WHERE play_session_id = @playSessionId",
                        new { playSessionId }, tx);
            });

        /// <summary> Starts a session for the game now and returns its id. The time is taken by the database. </summary>
        public static int Start(int gameId) =>
            Db.Run("Не вдалося розпочати сесію", () =>
            {
                using var connection = Db.Open();
                return connection.ExecuteScalar<int>(@"
                    INSERT INTO PlaySession (game_id, start_time, entry_method)
                    VALUES (@gameId, strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), 'Manual');
                    SELECT last_insert_rowid();", new { gameId });
            });

        /// <summary> Ends the open session now. Does nothing if it has already ended. </summary>
        public static void Stop(int playSessionId) =>
            Db.Run("Не вдалося завершити сесію", () =>
            {
                using var connection = Db.Open();
                connection.Execute(@"
                    UPDATE PlaySession
                    SET end_time = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                    WHERE play_session_id = @playSessionId AND end_time IS NULL",
                    new { playSessionId });
            });

        /// <summary> Filter parameters. The end date becomes an exclusive bound: the start of the next day. </summary>
        private static object ToFilterParams(SessionFilter filter) => new
        {
            GameCount = filter.GameIds.Count,
            GameIds = filter.GameIds.ToArray(),
            DateFrom = filter.DateFrom?.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
            DateToExclusive = filter.DateTo?.Date.AddDays(1).ToString(DateFormat, CultureInfo.InvariantCulture),
            DurationFrom = (long?)filter.DurationFrom?.TotalSeconds,
            DurationTo = (long?)filter.DurationTo?.TotalSeconds,
            Method = filter.Method?.ToString()
        };

        /// <summary> Session parameters. Times are written as text without fractions, as the table requires. </summary>
        private static object ToParams(PlaySession session) => new
        {
            session.PlaySessionId,
            session.GameId,
            StartTime = session.StartTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            EndTime = session.EndTime?.ToString(DateFormat, CultureInfo.InvariantCulture),
            EntryMethod = session.EntryMethod.ToString(),
            session.Note
        };
    }
}
