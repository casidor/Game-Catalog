using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Tag table. </summary>
    public static class TagRepository
    {
        /// <summary> Returns tags whose name contains the search text, ordered by name. </summary>
        public static List<Tag> GetAll(string? search = null) =>
            Db.Run("Не вдалося завантажити теги", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Tag>(@"
                    SELECT tag_id, tag_name, color
                    FROM Tag
                    WHERE instr(lower_ua(tag_name), lower_ua(@Search)) > 0
                    ORDER BY lower_ua(tag_name)",
                    new { Search = search?.Trim() ?? string.Empty }).AsList();
            });

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