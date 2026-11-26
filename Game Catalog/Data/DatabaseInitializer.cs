using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Game_Catalog.Data
{
    /// <summary>
    /// Creates the database file on first run: applies schema.sql, then fills the dictionaries
    /// with their initial data from genres.sql, all in one transaction.
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary> Does nothing if the database file already exists. </summary>
        public static void Initialize()
        {
            if (File.Exists(Db.DefaultPath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(Db.DefaultPath)!);

            try
            {
                using var connection = Db.Open();
                using var transaction = connection.BeginTransaction();
                foreach (var file in new[] { "schema.sql", "genres.sql" })
                {
                    using var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = ReadSqlFile(file);
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();

                using var walCmd = connection.CreateCommand();
                walCmd.CommandText = "PRAGMA journal_mode = WAL;";
                walCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                SqliteConnection.ClearAllPools();
                try { File.Delete(Db.DefaultPath); } catch { }

                if (ex is SqliteException se)
                    throw Db.Translate(se, "Не вдалося створити базу даних");
                throw;
            }
        }

        /// <summary> Reads an SQL file shipped next to the application in the Data folder. </summary>
        private static string ReadSqlFile(string name)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Database file {name} not found.", path);
            return File.ReadAllText(path);
        }
    }
}