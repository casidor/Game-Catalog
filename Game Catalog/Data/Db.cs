using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.IO;

namespace Game_Catalog.Data
{
    /// <summary> Single entry point for opening connections and translating database errors. </summary>
    public static class Db
    {
        /// <summary> Path to the SQLite database file in the user's AppData folder. </summary>
        public static readonly string DefaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "game_catalog_v2.db");

        private static string ConnectionString => $"Data Source={DefaultPath}";

        /// <summary> Ukrainian comparison rules used by the UKR_NOCASE collation. </summary>
        private static readonly CompareInfo UkrainianCompare = CultureInfo.GetCultureInfo("uk-UA").CompareInfo;

        /// <summary> One-time Dapper setup. Call once at application start. </summary>
        public static void Configure()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <summary> Opens a connection with foreign key enforcement enabled. </summary>
        public static SqliteConnection Open()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            RegisterExtensions(connection);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
            return connection;
        }

        /// <summary>
        /// Registers the application's SQL extensions on a connection:
        /// lower_ua() lowercases any text (the built-in lower() handles ASCII only),
        /// UKR_NOCASE orders and compares text by the Ukrainian alphabet, ignoring case.
        /// They exist only on connections opened by the application; the schema does not depend on them.
        /// </summary>
        public static void RegisterExtensions(SqliteConnection connection)
        {
            connection.CreateFunction("lower_ua", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);
            connection.CreateCollation("UKR_NOCASE",
                (a, b) => UkrainianCompare.Compare(a, b, CompareOptions.IgnoreCase));
        }

        /// <summary> Runs an action and converts SqliteException into DatabaseException. </summary>
        public static void Run(string action, Action work)
        {
            try { work(); }
            catch (SqliteException ex) { throw Translate(ex, action); }
        }

        /// <summary> Runs a function and converts SqliteException into DatabaseException. </summary>
        public static T Run<T>(string action, Func<T> work)
        {
            try { return work(); }
            catch (SqliteException ex) { throw Translate(ex, action); }
        }

        /// <summary> Runs work inside a single transaction; rolls back on any exception. </summary>
        public static void InTransaction(string action, Action<SqliteConnection, SqliteTransaction> work) =>
            Run(action, () =>
            {
                using var connection = Open();
                using var transaction = connection.BeginTransaction();
                work(connection, transaction);
                transaction.Commit();
            });

        /// <summary> Maps a SQLite error code to a DatabaseException. </summary>
        public static DatabaseException Translate(SqliteException ex, string action) =>
            ex.SqliteErrorCode switch
            {
                19 when ex.SqliteExtendedErrorCode is 2067 or 1555
                    => new(DatabaseErrorKind.Duplicate,
                           $"{action}: такий запис уже існує.", ex),
                19 when ex.Message.Contains("session_overlap")
                    => new(DatabaseErrorKind.Constraint,
                           $"{action}: сесія перетинається з іншою сесією цієї гри.", ex),
                19 when ex.Message.Contains("session_in_future")
                    => new(DatabaseErrorKind.Constraint,
                           $"{action}: сесія не може починатися чи завершуватися в майбутньому.", ex),
                19 when ex.Message.Contains("end_time > start_time")
                    => new(DatabaseErrorKind.Constraint,
                           $"{action}: сесія має завершуватися пізніше, ніж починається.", ex),
                19 when ex.Message.Contains("604800")
                    => new(DatabaseErrorKind.Constraint,
                           $"{action}: сесія не може тривати понад 7 діб.", ex),
                19 => new(DatabaseErrorKind.Constraint,
                           $"{action}: порушено обмеження бази даних.", ex),
                5 or 6 => new(DatabaseErrorKind.Unavailable,
                           $"{action}: база даних зайнята, спробуйте ще раз.", ex),
                8 => new(DatabaseErrorKind.Unavailable,
                           $"{action}: база даних доступна лише для читання.", ex),
                14 => new(DatabaseErrorKind.Unavailable,
                           $"{action}: не вдається відкрити файл бази даних.", ex),
                _ => new(DatabaseErrorKind.Unknown,
                           $"{action}: помилка бази даних (код {ex.SqliteErrorCode}).", ex)
            };
    }
}