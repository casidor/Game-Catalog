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
        /// <summary> Returns studios matching the filter, ordered by name. Filtering is done by the database. </summary>
        public static List<Studio> GetAll(StudioFilter filter) =>
            Db.Run("Не вдалося завантажити студії", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Studio>(@"
                    SELECT s.studio_id, s.studio_name, s.country, s.foundation_year,
                           s.main_genre, g.genre_name AS main_genre_name, s.website
                    FROM Studio s
                    LEFT JOIN Genre g ON g.genre_id = s.main_genre
                    WHERE (@CountryCount = 0 OR s.country    IN @Countries)
                      AND (@GenreCount   = 0 OR s.main_genre IN @GenreIds)
                      AND (@YearFrom IS NULL OR s.foundation_year >= @YearFrom)
                      AND (@YearTo   IS NULL OR s.foundation_year <= @YearTo)
                    ORDER BY lower_ua(s.studio_name), s.studio_id",
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

        /// <summary> Returns distinct countries that studios actually have, for the filter dropdown. </summary>
        public static List<string> GetCountries() =>
            Db.Run("Не вдалося завантажити країни", () =>
            {
                using var connection = Db.Open();
                return connection.Query<string>(@"
                    SELECT DISTINCT country
                    FROM Studio
                    WHERE country IS NOT NULL
                    ORDER BY lower_ua(country)").AsList();
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

        /// <summary> Returns false if the studio still has games (ON DELETE RESTRICT). </summary>
        public static bool Delete(int studioId) =>
            Db.Run("Не вдалося видалити студію", () =>
            {
                try
                {
                    using var connection = Db.Open();
                    connection.Execute("DELETE FROM Studio WHERE studio_id = @studioId", new { studioId });
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    return false;
                }
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