using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Studio table. </summary>
    public static class StudioRepository
    {
        /// <summary>
        /// Returns studios matching the filter in the order chosen in it.
        /// Filtering and sorting are done by the database; studios without a foundation year go last.
        /// </summary>
        public static List<Studio> GetAll(StudioFilter filter)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (filter.SortField, filter.SortDescending) switch
            {
                (StudioSortField.Year, false) =>
                    "s.foundation_year ASC NULLS LAST, s.studio_name COLLATE UKR_NOCASE ASC, s.studio_id ASC",
                (StudioSortField.Year, true) =>
                    "s.foundation_year DESC NULLS LAST, s.studio_name COLLATE UKR_NOCASE ASC, s.studio_id ASC",
                (_, true) => "s.studio_name COLLATE UKR_NOCASE DESC, s.studio_id ASC",
                _ => "s.studio_name COLLATE UKR_NOCASE ASC, s.studio_id ASC"
            };

            return Db.Run("Не вдалося завантажити студії", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Studio>($@"
                    SELECT s.studio_id, s.studio_name, s.country, s.foundation_year,
                           s.main_genre, g.genre_name AS main_genre_name, s.website
                    FROM Studio s
                    LEFT JOIN Genre g ON g.genre_id = s.main_genre
                    WHERE (@CountryCount = 0 OR s.country    IN @Countries)
                      AND (@GenreCount   = 0 OR s.main_genre IN @GenreIds)
                      AND (@YearFrom IS NULL OR s.foundation_year >= @YearFrom)
                      AND (@YearTo   IS NULL OR s.foundation_year <= @YearTo)
                    ORDER BY {orderBy}",
                    new
                    {
                        CountryCount = filter.Countries.Count,
                        Countries = filter.Countries.ToArray(),
                        GenreCount = filter.GenreIds.Count,
                        GenreIds = filter.GenreIds.ToArray(),
                        filter.YearFrom,
                        filter.YearTo
                    }).AsList();
            });
        }

        /// <summary>
        /// Returns ids of the studios whose name or country contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук студій", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT studio_id
                    FROM Studio
                    WHERE instr(lower_ua(studio_name), lower_ua(@Search)) > 0
                       OR instr(lower_ua(country), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

        /// <summary> Returns the studio with the given id, or null if it no longer exists. </summary>
        public static Studio? GetById(int studioId) =>
            Db.Run("Не вдалося завантажити студію", () =>
            {
                using var connection = Db.Open();
                return connection.QuerySingleOrDefault<Studio>(@"
            SELECT s.studio_id, s.studio_name, s.country, s.foundation_year,
                   s.main_genre, g.genre_name AS main_genre_name, s.website
            FROM Studio s
            LEFT JOIN Genre g ON g.genre_id = s.main_genre
            WHERE s.studio_id = @studioId",
                    new { studioId });
            });

        /// <summary> Returns distinct countries that studios actually have, for the filter dropdown. </summary>
        public static List<string> GetCountries() =>
            Db.Run("Не вдалося завантажити країни", () =>
            {
                using var connection = Db.Open();
                return connection.Query<string>(@"
                    SELECT DISTINCT country
                    FROM Studio
                    WHERE country IS NOT NULL
                    ORDER BY country COLLATE UKR_NOCASE").AsList();
            });

        /// <summary>
        /// Returns which of the given studios still have games (archived ones and DLC included),
        /// that is, which of them the database would refuse to delete.
        /// </summary>
        public static HashSet<int> GetIdsWithGames(IReadOnlyCollection<int> studioIds) =>
            Db.Run("Не вдалося перевірити ігри студій", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT DISTINCT developer_id
                    FROM Game
                    WHERE developer_id IN @studioIds",
                    new { studioIds }).ToHashSet();
            });

        /// <summary>
        /// Returns the games and DLC of the studio, newest first, games without a year last.
        /// A DLC takes the archive state of its main game, since it has none of its own.
        /// </summary>
        public static List<StudioGame> GetGames(int studioId) =>
            Db.Run("Не вдалося завантажити ігри студії", () =>
            {
                using var connection = Db.Open();
                // p is the main game of a DLC; for a standalone game it has no row, so all its columns are NULL.
                return connection.Query<StudioGame>(@"
                    SELECT g.game_id, g.parent_game_id, g.game_title, g.release_year,
                           g.game_status, g.personal_rating, g.icon_path,
                           COALESCE(p.is_archived, g.is_archived) AS is_archived,
                           p.game_title AS parent_title,
                           COALESCE(p.developer_id = g.developer_id, 0) AS is_nested
                    FROM Game g
                    LEFT JOIN Game p ON p.game_id = g.parent_game_id
                    WHERE g.developer_id = @studioId
                    ORDER BY g.release_year DESC NULLS LAST, g.game_title COLLATE UKR_NOCASE ASC, g.game_id ASC",
                    new { studioId }).AsList();
            });

        /// <summary> Inserts the studio and sets its generated id. </summary>
        public static void Insert(Studio studio) =>
            Db.Run("Не вдалося зберегти студію", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, studio);

                studio.StudioId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Studio (studio_name, country, foundation_year, main_genre, website)
                    VALUES (trim(@StudioName), NULLIF(trim(@Country), ''), @FoundationYear,
                            @MainGenre, NULLIF(trim(@Website), ''));
                    SELECT last_insert_rowid();", studio);
            });

        /// <summary> Updates all editable fields of an existing studio. </summary>
        public static void Update(Studio studio) =>
            Db.Run("Не вдалося оновити студію", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, studio);

                connection.Execute(@"
                    UPDATE Studio
                    SET studio_name = trim(@StudioName),
                        country = NULLIF(trim(@Country), ''),
                        foundation_year = @FoundationYear,
                        main_genre = @MainGenre,
                        website = NULLIF(trim(@Website), '')
                    WHERE studio_id = @StudioId", studio);
            });

        /// <summary>
        /// Deletes the studios with one statement, so all of them or none: a studio that still has games
        /// stops the whole statement (ON DELETE RESTRICT).
        /// So studios with games must be left out first, see <see cref="GetIdsWithGames"/>.
        /// </summary>
        public static void Delete(IReadOnlyCollection<int> studioIds) =>
            Db.Run("Не вдалося видалити студії", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Studio WHERE studio_id IN @studioIds", new { studioIds });
            });

        /// <summary> Throws a duplicate error if another studio has the same name, ignoring case and edge spaces. </summary>
        private static void EnsureNameUnique(SqliteConnection connection, Studio studio)
        {
            var exists = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM Studio
                    WHERE lower_ua(studio_name) = lower_ua(trim(@StudioName))
                      AND studio_id != @StudioId)", studio);

            if (exists)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    "Не вдалося зберегти студію: студія з такою назвою вже існує.");
        }
    }
}