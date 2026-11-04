using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Game table and its genre, platform and tag links. </summary>
    public static class GameRepository
    {
        /// <summary> Base select: the game, its studio name, total hours and days in the library. </summary>
        private const string BaseSelect = @"
            SELECT g.game_id, g.developer_id, s.studio_name AS developer_name, g.parent_game_id,
                   g.game_title, g.game_description, g.release_year, g.game_status, g.size_gb,
                   g.personal_rating, g.cover_image_path, g.background_image_path,
                   g.executable_path, g.install_path, g.icon_path, g.added_at, g.is_archived,
                   COALESCE((SELECT SUM((julianday(ps.end_time) - julianday(ps.start_time)) * 24.0)
                             FROM PlaySession ps
                             WHERE ps.game_id = g.game_id AND ps.end_time IS NOT NULL), 0) AS total_hours,
                   CAST(julianday('now', 'localtime') - julianday(g.added_at) AS INTEGER) AS days_in_library
            FROM Game g
            JOIN Studio s ON s.studio_id = g.developer_id";

        /// <summary> Returns games matching the filter, ordered by title. Filtering is done by the database. </summary>
        public static List<Game> GetAll(GameFilter filter) =>
            Db.Run("Не вдалося завантажити ігри", () =>
            {
                using var connection = Db.Open();
                var games = connection.Query<Game>($@"
                    SELECT * FROM ({BaseSelect}) x
                    WHERE (@IsArchived IS NULL OR x.is_archived = @IsArchived)
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
                    ORDER BY lower_ua(x.game_title), x.game_id",
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

        /// <summary> Returns the game with the given id, or null if it no longer exists. </summary>
        public static Game? GetById(int gameId) =>
            Db.Run("Не вдалося завантажити гру", () =>
            {
                using var connection = Db.Open();
                var game = connection.QuerySingleOrDefault<Game>(
                    BaseSelect + " WHERE g.game_id = @gameId", new { gameId });
                if (game != null)
                    LoadLinks(connection, new List<Game> { game });
                return game;
            });

        /// <summary> Inserts the game with its links in one transaction and sets its generated id. </summary>
        public static void Insert(Game game) =>
            Db.InTransaction("Не вдалося зберегти гру", (connection, tx) =>
            {
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

        /// <summary> Moves the game to the archive or back. </summary>
        public static void SetArchived(int gameId, bool archived) =>
            Db.Run("Не вдалося змінити архівацію гри", () =>
            {
                using var connection = Db.Open();
                connection.Execute("UPDATE Game SET is_archived = @value WHERE game_id = @gameId",
                    new { value = archived ? 1 : 0, gameId });
            });

        /// <summary> Deletes the game; sessions, goals, DLC and links are removed by the database. </summary>
        public static void Delete(int gameId) =>
            Db.Run("Не вдалося видалити гру", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Game WHERE game_id = @gameId", new { gameId });
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
                WHERE l.game_id IN @ids ORDER BY lower_ua(x.genre_name)", new { ids }).ToLookup(r => r.GameId);

            var platforms = connection.Query<LinkRow>(@"
                SELECT l.game_id, x.platform_id AS item_id, x.platform_name AS item_name, x.manufacturer AS item_extra
                FROM GamePlatform l JOIN Platform x ON x.platform_id = l.platform_id
                WHERE l.game_id IN @ids ORDER BY lower_ua(x.platform_name)", new { ids }).ToLookup(r => r.GameId);

            var tags = connection.Query<LinkRow>(@"
                SELECT l.game_id, x.tag_id AS item_id, x.tag_name AS item_name, x.color AS item_extra
                FROM GameTag l JOIN Tag x ON x.tag_id = l.tag_id
                WHERE l.game_id IN @ids ORDER BY lower_ua(x.tag_name)", new { ids }).ToLookup(r => r.GameId);

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
    }
}