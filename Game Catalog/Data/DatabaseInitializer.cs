using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Game_Catalog.Data
{
    /// <summary> Creates the database file and applies schema.sql on first run. </summary>
    public static class DatabaseInitializer
    {
        /// <summary> Does nothing if the database file already exists. </summary>
        public static void Initialize()
        {
            if (File.Exists(Db.DefaultPath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(Db.DefaultPath)!);

            try
            {
                var schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql");
                if (!File.Exists(schemaPath))
                    throw new FileNotFoundException("Database schema file not found.", schemaPath);

                var schemaSql = File.ReadAllText(schemaPath);

                using var connection = Db.Open();
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = schemaSql;
                cmd.ExecuteNonQuery();
                transaction.Commit();
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
    }
}