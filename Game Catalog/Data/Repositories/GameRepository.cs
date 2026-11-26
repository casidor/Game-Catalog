using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for games (read through the v_game view) and their genre, platform and tag links. </summary>
    public static class GameRepository
    {
        /// <summary>
        /// Returns standalone games (DLC are excluded) matching the filter in the order chosen in it.
        /// Filtering and sorting are done by the database; games without a value of the sort field go last.
        /// </summary>
        public static List<Game> GetAll(GameFilter filter)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (filter.SortField, filter.SortDescending) switch
            {
                (GameSortField.Year, false) =>
                    "x.release_year ASC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Year, true) =>
                    "x.release_year DESC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Rating, false) =>
                    "x.personal_rating ASC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Rating, true) =>
                    "x.personal_rating DESC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Size, false) =>
                    "x.size_gb ASC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Size, true) =>
                    "x.size_gb DESC NULLS LAST, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Hours, false) =>
                    "x.total_hours ASC, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (GameSortField.Hours, true) =>
                    "x.total_hours DESC, x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC",
                (_, true) => "x.game_title COLLATE UKR_NOCASE DESC, x.game_id ASC",
                _ => "x.game_title COLLATE UKR_NOCASE ASC, x.game_id ASC"
            };

            return Db.Run("Не вдалося завантажити ігри", () =>
            {
                using var connection = Db.Open();
                var games = connection.Query<Game>($@"
                    SELECT * FROM v_game x
                    WHERE x.parent_game_id IS NULL
                      AND (@IsArchived IS NULL OR x.is_archived = @IsArchived)
                      AND (@GenreCount = 0 OR EXISTS (
                            SELECT 1 FROM GameGenre l WHERE l.game_id = x.game_id AND l.genre_id IN @GenreIds))
                      AND (@PlatformCount = 0 OR EXISTS (
                            SELECT 1 FROM GamePlatform l WHERE l.game_id = x.game_id AND l.platform_id IN @PlatformIds))
                      AND (@TagCount = 0 OR EXISTS (
                            SELECT 1 FROM GameTag l WHERE l.game_id = x.game_id AND l.tag_id IN @TagIds))
                      AND (@DeveloperCount = 0 OR x.developer_id IN @DeveloperIds)
                      AND (@StatusCount = 0 OR x.game_status IN @Statuses)
                      AND (@YearFrom   IS NULL OR x.release_year    >= @YearFrom)
                      AND (@YearTo     IS NULL OR x.release_year    <= @YearTo)
                      AND (@RatingFrom IS NULL OR x.personal_rating >= @RatingFrom)
                      AND (@RatingTo   IS NULL OR x.personal_rating <= @RatingTo)
                      AND (@SizeFrom   IS NULL OR x.size_gb         >= @SizeFrom)
                      AND (@SizeTo     IS NULL OR x.size_gb         <= @SizeTo)
                      AND (@HoursFrom  IS NULL OR x.total_hours     >= @HoursFrom)
                      AND (@HoursTo    IS NULL OR x.total_hours     <= @HoursTo)
                    ORDER BY {orderBy}",
                    new
                    {
                        filter.IsArchived,
                        GenreCount = filter.GenreIds.Count,
                        GenreIds = filter.GenreIds.ToArray(),
                        PlatformCount = filter.PlatformIds.Count,
                        PlatformIds = filter.PlatformIds.ToArray(),
                        TagCount = filter.TagIds.Count,
                        TagIds = filter.TagIds.ToArray(),
                        DeveloperCount = filter.DeveloperIds.Count,
                        DeveloperIds = filter.DeveloperIds.ToArray(),
                        StatusCount = filter.Statuses.Count,
                        Statuses = filter.Statuses.Select(s => s.ToString()).ToArray(),
                        filter.YearFrom,
                        filter.YearTo,
                        filter.RatingFrom,
                        filter.RatingTo,
                        filter.SizeFrom,
                        filter.SizeTo,
                        filter.HoursFrom,
                        filter.HoursTo
                    }).AsList();

                LoadLinks(connection, games);
                return games;
            });
        }

        /// <summary>
        /// Returns ids of the games whose title or developer name contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук ігор", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT g.game_id
                    FROM Game g
                    JOIN Studio s ON s.studio_id = g.developer_id
                    WHERE instr(lower_ua(g.game_title), lower_ua(@Search)) > 0
                       OR instr(lower_ua(s.studio_name), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

        /// <summary> Returns the game with the given id, or null if it no longer exists. </summary>
        public static Game? GetById(int gameId) =>
            Db.Run("Не вдалося завантажити гру", () =>
            {
                using var connection = Db.Open();
                var game = connection.QuerySingleOrDefault<Game>(
                    "SELECT * FROM v_game WHERE game_id = @gameId", new { gameId });
                if (game != null)
                    LoadLinks(connection, new List<Game> { game });
                return game;
            });

        /// <summary>
        /// Returns the DLC of the game, oldest first, then by title; DLC without a year go last.
        /// DLC have no genres, platforms or tags of their own, so links are not loaded.
        /// </summary>
        public static List<Game> GetDlcs(int parentGameId) =>
            Db.Run("Не вдалося завантажити доповнення", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Game>(@"
                    SELECT * FROM v_game
                    WHERE parent_game_id = @parentGameId
                    ORDER BY release_year ASC NULLS LAST, game_title COLLATE UKR_NOCASE ASC, game_id ASC",
                    new { parentGameId }).AsList();
            });

        /// <summary> Inserts the game with its links in one transaction and sets its generated id. </summary>
        public static void Insert(Game game) =>
            Db.InTransaction("Не вдалося зберегти гру", (connection, tx) =>
            {
                EnsureTitleUnique(connection, tx, game);
                EnsureInstallPathUnique(connection, tx, game);

                game.GameId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Game (developer_id, parent_game_id, game_title, game_description,
                                      release_year, game_status, size_gb, personal_rating,
                                      cover_image_path, background_image_path, executable_path,
                                      install_path, icon_path, is_archived)
                    VALUES (@DeveloperId, @ParentGameId, trim(@GameTitle), NULLIF(trim(@GameDescription), ''),
                            @ReleaseYear, @GameStatus, @SizeGB, @PersonalRating,
                            NULLIF(trim(@CoverImagePath), ''), NULLIF(trim(@BackgroundImagePath), ''),
                            NULLIF(trim(@ExecutablePath), ''), NULLIF(trim(@InstallPath), ''),
                            NULLIF(trim(@IconPath), ''), @IsArchived);
                    SELECT last_insert_rowid();", ToParams(game), tx);

                SaveLinks(connection, tx, game);
            });

        /// <summary> Updates the game and replaces its links in one transaction. </summary>
        public static void Update(Game game) =>
            Db.InTransaction("Не вдалося оновити гру", (connection, tx) =>
            {
                EnsureTitleUnique(connection, tx, game);
                EnsureInstallPathUnique(connection, tx, game);

                connection.Execute(@"
                    UPDATE Game
                    SET developer_id = @DeveloperId,
                        parent_game_id = @ParentGameId,
                        game_title = trim(@GameTitle),
                        game_description = NULLIF(trim(@GameDescription), ''),
                        release_year = @ReleaseYear,
                        game_status = @GameStatus,
                        size_gb = @SizeGB,
                        personal_rating = @PersonalRating,
                        cover_image_path = NULLIF(trim(@CoverImagePath), ''),
                        background_image_path = NULLIF(trim(@BackgroundImagePath), ''),
                        executable_path = NULLIF(trim(@ExecutablePath), ''),
                        install_path = NULLIF(trim(@InstallPath), ''),
                        icon_path = NULLIF(trim(@IconPath), ''),
                        is_archived = @IsArchived
                    WHERE game_id = @GameId", ToParams(game), tx);

                SaveLinks(connection, tx, game);
            });

        /// <summary> Sets only the image paths of the game. Empty paths are stored as NULL. </summary>
        public static void UpdateImages(int gameId, string coverImagePath, string backgroundImagePath, string iconPath) =>
            Db.Run("Не вдалося зберегти зображення гри", () =>
            {
                using var connection = Db.Open();
                connection.Execute(@"
                    UPDATE Game
                    SET cover_image_path = NULLIF(trim(@coverImagePath), ''),
                        background_image_path = NULLIF(trim(@backgroundImagePath), ''),
                        icon_path = NULLIF(trim(@iconPath), '')
                    WHERE game_id = @gameId",
                    new { gameId, coverImagePath, backgroundImagePath, iconPath });
            });

        /// <summary>
        /// Deletes the games with one statement, so all of them or none.
        /// Their sessions, goals, achievements, DLC and links are removed by the database (ON DELETE CASCADE).
        /// </summary>
        public static void Delete(IReadOnlyCollection<int> gameIds) =>
            Db.Run("Не вдалося видалити ігри", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Game WHERE game_id IN @gameIds", new { gameIds });
            });

        /// <summary>
        /// Moves the games to the archive in one transaction. A running session would stop archiving
        /// (trigger game_has_active_session), so the sessions still running in these games are ended first.
        /// </summary>
        public static void Archive(IReadOnlyCollection<int> gameIds) =>
            Db.InTransaction("Не вдалося перенести ігри в архів", (connection, tx) =>
            {
                connection.Execute(@"
                    UPDATE PlaySession
                    SET end_time = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                    WHERE game_id IN @gameIds AND end_time IS NULL",
                    new { gameIds }, tx);

                connection.Execute("UPDATE Game SET is_archived = 1 WHERE game_id IN @gameIds",
                    new { gameIds }, tx);
            });

        /// <summary> Returns the games from the archive to the library with one statement. </summary>
        public static void Restore(IReadOnlyCollection<int> gameIds) =>
            Db.Run("Не вдалося відновити ігри", () =>
            {
                using var connection = Db.Open();
                connection.Execute("UPDATE Game SET is_archived = 0 WHERE game_id IN @gameIds", new { gameIds });
            });

        /// <summary> Deletes the DLC in one transaction. Rows that are not DLC are left alone. </summary>
        public static void DeleteDlcs(IReadOnlyCollection<int> dlcIds) =>
            Db.InTransaction("Не вдалося видалити доповнення", (connection, tx) =>
            {
                foreach (var dlcId in dlcIds)
                    connection.Execute(
                        "DELETE FROM Game WHERE game_id = @dlcId AND parent_game_id IS NOT NULL",
                        new { dlcId }, tx);
            });

        /// <summary> Parameters of the Game table. The status is sent as text because the column is TEXT. </summary>
        private static object ToParams(Game game) => new
        {
            game.GameId,
            game.DeveloperId,
            game.ParentGameId,
            game.GameTitle,
            game.GameDescription,
            game.ReleaseYear,
            GameStatus = game.GameStatus.ToString(),
            game.SizeGB,
            game.PersonalRating,
            game.CoverImagePath,
            game.BackgroundImagePath,
            game.ExecutablePath,
            game.InstallPath,
            game.IconPath,
            IsArchived = game.IsArchived ? 1 : 0
        };

        /// <summary> Replaces the genre, platform and tag links of the game. </summary>
        private static void SaveLinks(SqliteConnection connection, SqliteTransaction tx, Game game)
        {
            var id = game.GameId;

            connection.Execute("DELETE FROM GameGenre WHERE game_id = @id", new { id }, tx);
            connection.Execute("INSERT INTO GameGenre (game_id, genre_id) VALUES (@id, @itemId)",
                game.Genres.Select(x => x.GenreId).Distinct().Select(itemId => new { id, itemId }), tx);

            connection.Execute("DELETE FROM GamePlatform WHERE game_id = @id", new { id }, tx);
            connection.Execute("INSERT INTO GamePlatform (game_id, platform_id) VALUES (@id, @itemId)",
                game.Platforms.Select(x => x.PlatformId).Distinct().Select(itemId => new { id, itemId }), tx);

            connection.Execute("DELETE FROM GameTag WHERE game_id = @id", new { id }, tx);
            connection.Execute("INSERT INTO GameTag (game_id, tag_id) VALUES (@id, @itemId)",
                game.Tags.Select(x => x.TagId).Distinct().Select(itemId => new { id, itemId }), tx);
        }

        /// <summary> One row of a link query: the game id and the linked item. </summary>
        private sealed class LinkRow
        {
            public int GameId { get; set; }
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public string? ItemExtra { get; set; }
        }

        /// <summary> Fills genres, platforms and tags of the given games with three queries. </summary>
        private static void LoadLinks(SqliteConnection connection, List<Game> games)
        {
            if (games.Count == 0) return;
            var ids = games.Select(g => g.GameId).ToArray();

            var genres = connection.Query<LinkRow>(@"
                SELECT l.game_id, x.genre_id AS item_id, x.genre_name AS item_name, NULL AS item_extra
                FROM GameGenre l JOIN Genre x ON x.genre_id = l.genre_id
                WHERE l.game_id IN @ids ORDER BY x.genre_name COLLATE UKR_NOCASE", new { ids }).ToLookup(r => r.GameId);

            var platforms = connection.Query<LinkRow>(@"
                SELECT l.game_id, x.platform_id AS item_id, x.platform_name AS item_name, x.manufacturer AS item_extra
                FROM GamePlatform l JOIN Platform x ON x.platform_id = l.platform_id
                WHERE l.game_id IN @ids ORDER BY x.platform_name COLLATE UKR_NOCASE", new { ids }).ToLookup(r => r.GameId);

            var tags = connection.Query<LinkRow>(@"
                SELECT l.game_id, x.tag_id AS item_id, x.tag_name AS item_name, x.color AS item_extra
                FROM GameTag l JOIN Tag x ON x.tag_id = l.tag_id
                WHERE l.game_id IN @ids ORDER BY x.tag_name COLLATE UKR_NOCASE", new { ids }).ToLookup(r => r.GameId);

            foreach (var game in games)
            {
                game.Genres = genres[game.GameId]
                    .Select(r => new Genre { GenreId = r.ItemId, GenreName = r.ItemName }).ToList();
                game.Platforms = platforms[game.GameId]
                    .Select(r => new Platform { PlatformId = r.ItemId, PlatformName = r.ItemName, Manufacturer = r.ItemExtra ?? string.Empty }).ToList();
                game.Tags = tags[game.GameId]
                    .Select(r => new Tag { TagId = r.ItemId, TagName = r.ItemName, Color = r.ItemExtra ?? string.Empty }).ToList();
            }
        }

        /// <summary>
        /// Throws a duplicate error if another game has the same title, developer and release year.
        /// Repeats the unique index of the schema, but compares titles ignoring case in any alphabet,
        /// while the index ignores case of Latin letters only.
        /// </summary>
        private static void EnsureTitleUnique(SqliteConnection connection, SqliteTransaction tx, Game game)
        {
            var exists = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM Game
                    WHERE lower_ua(game_title) = lower_ua(trim(@GameTitle))
                      AND developer_id = @DeveloperId
                      AND COALESCE(release_year, 0) = COALESCE(@ReleaseYear, 0)
                      AND game_id != @GameId)",
                new { game.GameTitle, game.DeveloperId, game.ReleaseYear, game.GameId }, tx);

            if (exists)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    "Не вдалося зберегти гру: гра з такою назвою, студією й роком уже існує.");
        }

        /// <summary> Throws a duplicate error if another game already uses the same install folder. </summary>
        private static void EnsureInstallPathUnique(SqliteConnection connection, SqliteTransaction tx, Game game)
        {
            var owner = connection.ExecuteScalar<string?>(@"
                SELECT game_title FROM Game
                WHERE install_path = NULLIF(trim(@InstallPath), '')
                  AND game_id != @GameId", new { game.InstallPath, game.GameId }, tx);

            if (owner != null)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    $"Не вдалося зберегти гру: ця тека вже вказана для гри «{owner}».");
        }
    }
}