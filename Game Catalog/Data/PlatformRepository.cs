using Dapper;
using Game_Catalog.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for the Platform table. </summary>
    public static class PlatformRepository
    {
        /// <summary> Returns platforms whose name or manufacturer contains the search text, ordered by name. </summary>
        public static List<Platform> GetAll(string? search = null) =>
            Db.Run("Не вдалося завантажити платформи", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Platform>(@"
                    SELECT platform_id, platform_name, manufacturer, platform_icon_path
                    FROM Platform
                    WHERE instr(lower_ua(platform_name), lower_ua(@Search)) > 0
                       OR instr(lower_ua(COALESCE(manufacturer, '')), lower_ua(@Search)) > 0
                    ORDER BY lower_ua(platform_name)",
                    new { Search = search?.Trim() ?? string.Empty }).AsList();
            });

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