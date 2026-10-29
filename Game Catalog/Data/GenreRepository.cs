using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Genre table. </summary>
    public static class GenreRepository
    {
        /// <summary> Returns genres whose name contains the search text, ordered by name. </summary>
        public static List<Genre> GetAll(string? search = null) =>
            Db.Run("Не вдалося завантажити жанри", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Genre>(@"
                    SELECT genre_id, genre_name, genre_description
                    FROM Genre
                    WHERE instr(lower_ua(genre_name), lower_ua(@Search)) > 0
                    ORDER BY lower_ua(genre_name)",
                    new { Search = search?.Trim() ?? string.Empty }).AsList();
            });

        /// <summary> Inserts the genre and sets its generated id. </summary>
        public static void Insert(Genre genre) =>
            Db.Run("Не вдалося зберегти жанр", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, genre);

                genre.GenreId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Genre (genre_name, genre_description)
                    VALUES (trim(@GenreName), NULLIF(trim(@GenreDescription), ''));
                    SELECT last_insert_rowid();", genre);
            });

        /// <summary> Updates name and description of an existing genre. </summary>
        public static void Update(Genre genre) =>
            Db.Run("Не вдалося оновити жанр", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, genre);

                connection.Execute(@"
                    UPDATE Genre
                    SET genre_name = trim(@GenreName),
                        genre_description = NULLIF(trim(@GenreDescription), '')
                    WHERE genre_id = @GenreId", genre);
            });

        /// <summary> Returns false if the genre is still referenced by a game or a studio. </summary>
        public static bool Delete(int genreId) =>
            Db.Run("Не вдалося видалити жанр", () =>
            {
                try
                {
                    using var connection = Db.Open();
                    connection.Execute("DELETE FROM Genre WHERE genre_id = @genreId", new { genreId });
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    return false;
                }
            });

        /// <summary> Throws a duplicate error if another genre has the same name, ignoring case and edge spaces. </summary>
        private static void EnsureNameUnique(SqliteConnection connection, Genre genre)
        {
            var exists = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM Genre
                    WHERE lower_ua(genre_name) = lower_ua(trim(@GenreName))
                      AND genre_id != @GenreId)", genre);

            if (exists)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    "Не вдалося зберегти жанр: жанр з такою назвою вже існує.");
        }
    }
}