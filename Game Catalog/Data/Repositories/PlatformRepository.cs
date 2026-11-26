using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Platform table. </summary>
    public static class PlatformRepository
    {
        /// <summary>
        /// Returns all platforms in the chosen order. The sorting is done by the database;
        /// platforms without a manufacturer go last when sorted by it.
        /// </summary>
        public static List<Platform> GetAll(
            DictionarySortField sortField = DictionarySortField.Name, bool descending = false)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (sortField, descending) switch
            {
                (DictionarySortField.Manufacturer, false) =>
                    "manufacturer COLLATE UKR_NOCASE ASC NULLS LAST, platform_name COLLATE UKR_NOCASE ASC, platform_id ASC",
                (DictionarySortField.Manufacturer, true) =>
                    "manufacturer COLLATE UKR_NOCASE DESC NULLS LAST, platform_name COLLATE UKR_NOCASE ASC, platform_id ASC",
                (_, true) => "platform_name COLLATE UKR_NOCASE DESC, platform_id ASC",
                _ => "platform_name COLLATE UKR_NOCASE ASC, platform_id ASC"
            };

            return Db.Run("Не вдалося завантажити платформи", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Platform>($@"
                    SELECT platform_id, platform_name, manufacturer, platform_icon_path
                    FROM Platform
                    ORDER BY {orderBy}").AsList();
            });
        }

        /// <summary>
        /// Returns ids of the platforms whose name or manufacturer contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук платформ", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT platform_id
                    FROM Platform
                    WHERE instr(lower_ua(platform_name), lower_ua(@Search)) > 0
                       OR instr(lower_ua(manufacturer), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

        /// <summary> Inserts the platform and sets its generated id. </summary>
        public static void Insert(Platform platform) =>
            Db.Run("Не вдалося зберегти платформу", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, platform);

                platform.PlatformId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Platform (platform_name, manufacturer)
                    VALUES (trim(@PlatformName), NULLIF(trim(@Manufacturer), ''));
                    SELECT last_insert_rowid();", platform);
            });

        /// <summary> Updates name and manufacturer of an existing platform. </summary>
        public static void Update(Platform platform) =>
            Db.Run("Не вдалося оновити платформу", () =>
            {
                using var connection = Db.Open();
                EnsureNameUnique(connection, platform);

                connection.Execute(@"
                    UPDATE Platform
                    SET platform_name = trim(@PlatformName),
                        manufacturer = NULLIF(trim(@Manufacturer), '')
                    WHERE platform_id = @PlatformId", platform);
            });

        /// <summary> Returns false if the platform is still referenced by a game. </summary>
        public static bool Delete(int platformId) =>
            Db.Run("Не вдалося видалити платформу", () =>
            {
                try
                {
                    using var connection = Db.Open();
                    connection.Execute("DELETE FROM Platform WHERE platform_id = @platformId", new { platformId });
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    return false;
                }
            });

        /// <summary>
        /// Returns which of the given platforms are still linked to a game,
        /// that is, which of them the database would refuse to delete.
        /// </summary>
        public static HashSet<int> GetIdsInUse(IReadOnlyCollection<int> platformIds) =>
            Db.Run("Не вдалося перевірити використання платформ", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT DISTINCT platform_id
                    FROM GamePlatform
                    WHERE platform_id IN @platformIds",
                    new { platformIds }).ToHashSet();
            });

        /// <summary>
        /// Deletes the platforms with one statement, so all of them or none: a platform still linked to a game
        /// stops the whole statement (ON DELETE RESTRICT).
        /// So platforms in use must be left out first, see <see cref="GetIdsInUse"/>.
        /// </summary>
        public static void Delete(IReadOnlyCollection<int> platformIds) =>
            Db.Run("Не вдалося видалити платформи", () =>
            {
                using var connection = Db.Open();
                connection.Execute("DELETE FROM Platform WHERE platform_id IN @platformIds", new { platformIds });
            });

        /// <summary> Throws a duplicate error if another platform has the same name, ignoring case and edge spaces. </summary>
        private static void EnsureNameUnique(SqliteConnection connection, Platform platform)
        {
            var exists = connection.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM Platform
                    WHERE lower_ua(platform_name) = lower_ua(trim(@PlatformName))
                      AND platform_id != @PlatformId)", platform);

            if (exists)
                throw new DatabaseException(DatabaseErrorKind.Duplicate,
                    "Не вдалося зберегти платформу: платформа з такою назвою вже існує.");
        }
    }
}