using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Tag table. </summary>
    public static class TagRepository
    {
        /// <summary> Returns all tags ordered by name. The sorting is done by the database. </summary>
        public static List<Tag> GetAll(bool descending = false)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = descending
                ? "tag_name COLLATE UKR_NOCASE DESC, tag_id ASC"
                : "tag_name COLLATE UKR_NOCASE ASC, tag_id ASC";

            return Db.Run("Не вдалося завантажити теги", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Tag>($@"
                    SELECT tag_id, tag_name, color
                    FROM Tag
                    ORDER BY {orderBy}").AsList();
            });
        }

        /// <summary>
        /// Returns ids of the tags whose name contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук тегів", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT tag_id
                    FROM Tag
                    WHERE instr(lower_ua(tag_name), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

        /// <summary> Inserts the tag and sets its generated id. </summary>
        public static void Insert(Tag tag) =>
            Db.Run("Не вдалося зберегти тег", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, tag);

                tag.TagId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Tag (tag_name, color)
                    VALUES (trim(@TagName), NULLIF(trim(@Color), ''));
                    SELECT last_insert_rowid();", tag);
            });

        /// <summary> Updates name and color of an existing tag. </summary>
        public static void Update(Tag tag) =>
            Db.Run("Не вдалося оновити тег", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, tag);

                connection.Execute(@"
                    UPDATE Tag
                    SET tag_name = trim(@TagName),
                        color = NULLIF(trim(@Color), '')
                    WHERE tag_id = @TagId", tag);
            });

        /// <summary> Deletes the tag; its links with games are removed by the database, the games stay. </summary>
        public static void Delete(int tagId) =>
            Db.Run("Не вдалося видалити тег", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Tag WHERE tag_id = @tagId", new { tagId });
            });

        /// <summary>
        /// Deletes the tags with one statement. A tag never blocks deleting:
        /// its links with games are removed by the database (ON DELETE CASCADE), the games stay.
        /// </summary>
        public static void Delete(IReadOnlyCollection<int> tagIds) =>
            Db.Run("Не вдалося видалити теги", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Tag WHERE tag_id IN @tagIds", new { tagIds });
            });

        /// <summary> Throws a duplicate error if another tag has the same name, ignoring case and edge spaces. </summary>
        private static void EnsureNameUnique(SqliteConnection connection, Tag tag)
        {
            var exists = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM Tag
                    WHERE lower_ua(tag_name) = lower_ua(trim(@TagName))
                      AND tag_id != @TagId)", tag);

            if (exists)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    "Не вдалося зберегти тег: тег з такою назвою вже існує.");
        }
    }
}