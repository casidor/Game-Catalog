using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Data
{
    /// <summary> Result of an arbitrary SQL query: column names and string rows. </summary>
    public sealed record QueryResult(List<string> Columns, List<string?[]> Rows, int RowsAffected);

    /// <summary> Runs arbitrary SQL for the developer query editor. </summary>
    public static class QueryRunner
    {
        /// <summary>
        /// Executes the SQL against the current database.
        /// In read-only mode the connection is opened with Mode=ReadOnly, so modifying statements fail.
        /// Raw SqliteException is not translated so the editor can show the original message.
        /// </summary>
        public static QueryResult Execute(string sql, bool allowModify)
        {
            var connString = new SqliteConnectionStringBuilder
            {
                DataSource = Db.DefaultPath,
                Mode = allowModify ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
            }.ToString();

            using var connection = new SqliteConnection(connString);
            connection.Open();

            connection.CreateFunction("lower_ua", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            var rows = new List<string?[]>();
            while (reader.Read())
            {
                var row = new string?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i)
                        ? null
                        : System.Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                rows.Add(row);
            }

            return new QueryResult(columns, rows, reader.RecordsAffected);
        }
    }
}