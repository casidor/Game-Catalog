using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Genre table. </summary>
    public static class GenreRepository
    {
        /// <summary> Returns all genres ordered by name. The sorting is done by the database. </summary>
        public static List<Genre> GetAll(bool descending = false)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = descending
                ? "genre_name COLLATE UKR_NOCASE DESC, genre_id ASC"
                : "genre_name COLLATE UKR_NOCASE ASC, genre_id ASC";

            return Db.Run("Не вдалося завантажити жанри", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Genre>($@"
                    SELECT genre_id, genre_name, genre_description
                    FROM Genre
                    ORDER BY {orderBy}").AsList();
            });
        }

        /// <summary>
        /// Returns genres that at least one standalone game (DLC excluded) of the library or the archive has,
        /// for the games filter.
        /// </summary>
        public static List<Genre> GetUsedInGames(bool isArchived) =>
            Db.Run("Не вдалося завантажити жанри", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Genre>(@"
                    SELECT g.genre_id, g.genre_name, g.genre_description
                    FROM Genre g
                    WHERE EXISTS (
                        SELECT 1
                        FROM GameGenre l
                        JOIN Game x ON x.game_id = l.game_id
                        WHERE l.genre_id = g.genre_id
                          AND x.parent_game_id IS NULL
                          AND x.is_archived = @isArchived)
                    ORDER BY g.genre_name COLLATE UKR_NOCASE ASC, g.genre_id ASC",
                    new { isArchived }).AsList();
            });

        /// <summary> Returns genres that are the main genre of at least one studio, for the studios filter. </summary>
        public static List<Genre> GetUsedByStudios() =>
            Db.Run("Не вдалося завантажити жанри", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Genre>(@"
                    SELECT g.genre_id, g.genre_name, g.genre_description
                    FROM Genre g
                    WHERE EXISTS (SELECT 1 FROM Studio s WHERE s.main_genre_id = g.genre_id)
                    ORDER BY g.genre_name COLLATE UKR_NOCASE ASC, g.genre_id ASC").AsList();
            });

        /// <summary>
        /// Returns ids of the genres whose name contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук жанрів", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT genre_id
                    FROM Genre
                    WHERE instr(lower_ua(genre_name), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

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

        /// <summary>
        /// Returns which of the given genres are still in use, linked to a game or set as the main genre of a studio,
        /// that is, which of them the database would refuse to delete.
        /// </summary>
        public static HashSet<int> GetIdsInUse(IReadOnlyCollection<int> genreIds) =>
            Db.Run("Не вдалося перевірити використання жанрів", () =>
            {
                using var connection = Db.Open();
                // UNION also removes the duplicates: a genre used both ways is returned once.
                return connection.Query<int>(@"
                    SELECT genre_id FROM GameGenre WHERE genre_id IN @genreIds
                    UNION
                    SELECT main_genre_id FROM Studio WHERE main_genre_id IN @genreIds",
                    new { genreIds }).ToHashSet();
            });

        /// <summary>
        /// Deletes the genres with one statement, so all of them or none: a genre still in use
        /// stops the whole statement (ON DELETE RESTRICT).
        /// So genres in use must be left out first, see <see cref="GetIdsInUse"/>.
        /// </summary>
        public static void Delete(IReadOnlyCollection<int> genreIds) =>
            Db.Run("Не вдалося видалити жанри", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Genre WHERE genre_id IN @genreIds", new { genreIds });
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