using Microsoft.Data.Sqlite;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game_Catalog.Services
{
    /// <summary>
    /// Handles all SQLite persistence: schema initialization and CRUD for Studio, Game, PlaySession.
    /// </summary>
    public static class DatabaseService
    {
        public static readonly string DefaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameCatalog", "game_catalog.db");

        private static string ConnectionString => $"Data Source={DefaultPath}";

        private static DatabaseException Translate(SqliteException ex, string action) =>
        ex.SqliteErrorCode switch
        {
            19 when ex.SqliteExtendedErrorCode is 2067 or 1555
               => new(DatabaseErrorKind.Duplicate,
                      $"{action}: такий запис уже існує.", ex),
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

        private static void Execute(string action, Action work)
        {
            try { work(); }
            catch (SqliteException ex) { throw Translate(ex, action); }
        }

        private static T Execute<T>(string action, Func<T> work)
        {
            try { return work(); }
            catch (SqliteException ex) { throw Translate(ex, action); }
        }

        /// <summary> Creates the database file and schema on first run; otherwise does nothing. </summary>
        public static void Initialize()
        {
            bool isNewDatabase = !File.Exists(DefaultPath);
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultPath)!);

            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    using (var pragmaCmd = connection.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                        pragmaCmd.ExecuteNonQuery();
                    }
                    if (isNewDatabase)
                    {
                        string schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql");
                        if (!File.Exists(schemaPath))
                            throw new FileNotFoundException("Не знайдено файл схеми бази даних.", schemaPath);

                        string schemaSql = File.ReadAllText(schemaPath);
                        using var transaction = connection.BeginTransaction();
                        using var schemaCmd = connection.CreateCommand();
                        schemaCmd.Transaction = transaction;
                        schemaCmd.CommandText = schemaSql;
                        schemaCmd.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                if (isNewDatabase)
                {
                    SqliteConnection.ClearAllPools();
                    try { File.Delete(DefaultPath); } catch { }
                }
                if (ex is SqliteException se)
                    throw Translate(se, "Не вдалося створити базу даних");
                throw;
            }
        }

        private static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
            return connection;
        }

        private static object ToDb(string value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

        #region Studio

        public static List<Studio> GetStudios()
        {
            var result = new List<Studio>();

            using var connection = OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT studio_id, name, country, foundation_year, main_genre, website
                                 FROM Studio";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Studio
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Country = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    FoundationYear = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    MainGenre = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Website = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }
            return result;
        }

        public static void InsertStudio(Studio studio) =>
            Execute("Не вдалося зберегти студію", () =>
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Studio (name, country, foundation_year, main_genre, website)
                                 VALUES (@name, @country, @foundation_year, @main_genre, @website)";

                cmd.Parameters.AddWithValue("@name", studio.Name);
                cmd.Parameters.AddWithValue("@country", ToDb(studio.Country));
                cmd.Parameters.AddWithValue("@foundation_year", (object?)studio.FoundationYear ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@main_genre", ToDb(studio.MainGenre));
                cmd.Parameters.AddWithValue("@website", ToDb(studio.Website));
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
                cmd.CommandText = "SELECT last_insert_rowid()";
                studio.Id = (int)(long)cmd.ExecuteScalar()!;
            });

        public static void UpdateStudio(Studio studio) =>
            Execute("Не вдалося оновити студію", () =>
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Studio
                                 SET name = @name, country = @country, foundation_year = @foundation_year,
                                     main_genre = @main_genre, website = @website
                                 WHERE studio_id = @id";

                cmd.Parameters.AddWithValue("@name", studio.Name);
                cmd.Parameters.AddWithValue("@country", ToDb(studio.Country));
                cmd.Parameters.AddWithValue("@foundation_year", (object?)studio.FoundationYear ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@main_genre", ToDb(studio.MainGenre));
                cmd.Parameters.AddWithValue("@website", ToDb(studio.Website));
                cmd.Parameters.AddWithValue("@id", studio.Id);
                cmd.ExecuteNonQuery();
            });

        /// <summary> Returns false without throwing if the studio still has games referencing it (ON DELETE RESTRICT). </summary>
        public static bool DeleteStudio(int studioId) =>
            Execute("Не вдалося видалити студію", () =>
            {
                try
                {
                    using var connection = OpenConnection();
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "DELETE FROM Studio WHERE studio_id = @id";
                    cmd.Parameters.AddWithValue("@id", studioId);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    return false;
                }
            });

        #endregion

        #region Game

        public static List<Game> GetGames(IEnumerable<Studio> studios)
        {
            var studioById = studios.ToDictionary(s => s.Id);
            var result = new List<Game>();

            using var connection = OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT game_id, developer_id, parent_game_id, title, genre, platform,
                                       description, release_year, status, size_gb, personal_rating,
                                       cover_image_path, executable_path, icon_path,
                                       added_at, is_archived
                                FROM Game";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var game = new Game
                {
                    Id = reader.GetInt32(0),
                    DeveloperId = reader.GetInt32(1),
                    ParentGameId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Title = reader.GetString(3),
                    Genre = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Platform = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Description = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    ReleaseYear = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    Status = Enum.TryParse<GameStatus>(reader.GetString(8), out var status) ? status : GameStatus.Planned,
                    SizeGB = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
                    PersonalRating = reader.GetInt32(10),
                    CoverImagePath = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                    ExecutablePath = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    IconPath = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    AddedAt = reader.GetDateTime(14),
                    IsArchived = reader.GetBoolean(15)
                };

                if (studioById.TryGetValue(game.DeveloperId, out var developer))
                    game.Developer = developer;

                result.Add(game);
            }
            return result;
        }

        public static void InsertGame(Game game) =>
            Execute("Не вдалося зберегти гру", () =>
            {
                if (game.Developer != null)
                    game.DeveloperId = game.Developer.Id;

                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Game (developer_id, parent_game_id, title, genre, platform,
                                                   description, release_year, status, size_gb, personal_rating,
                                                   cover_image_path, executable_path, icon_path,
                                                   added_at, is_archived)
                                 VALUES (@developer_id, @parent_game_id, @title, @genre, @platform,
                                         @description, @release_year, @status, @size_gb, @personal_rating,
                                         @cover_image_path, @executable_path, @icon_path,
                                         @added_at, @is_archived)";

                AddGameParameters(cmd, game);
                cmd.Parameters.AddWithValue("@added_at", game.AddedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
                cmd.CommandText = "SELECT last_insert_rowid()";
                game.Id = (int)(long)cmd.ExecuteScalar()!;
            });

        public static void UpdateGame(Game game) =>
            Execute("Не вдалося оновити гру", () =>
            {
                if (game.Developer != null)
                    game.DeveloperId = game.Developer.Id;

                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Game
                                 SET developer_id = @developer_id, parent_game_id = @parent_game_id,
                                     title = @title, genre = @genre, platform = @platform,
                                     description = @description, release_year = @release_year,
                                     status = @status, size_gb = @size_gb, personal_rating = @personal_rating,
                                     cover_image_path = @cover_image_path,
                                     executable_path = @executable_path, icon_path = @icon_path,
                                     is_archived = @is_archived
                                 WHERE game_id = @id";

                AddGameParameters(cmd, game);
                cmd.Parameters.AddWithValue("@id", game.Id);
                cmd.ExecuteNonQuery();
            });

        public static void SetArchived(int gameId, bool archived) =>
            Execute("Не вдалося змінити архівацію гри", () =>
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Game SET is_archived = @archived WHERE game_id = @id";
                cmd.Parameters.AddWithValue("@archived", archived ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", gameId);
                cmd.ExecuteNonQuery();
            });
        public static void DeleteGame(int gameId) =>
            Execute("Не вдалося видалити гру", () =>
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Game WHERE game_id = @id";
                cmd.Parameters.AddWithValue("@id", gameId);
                cmd.ExecuteNonQuery();
            });

        private static void AddGameParameters(SqliteCommand cmd, Game game)
        {
            cmd.Parameters.AddWithValue("@developer_id", game.DeveloperId);
            cmd.Parameters.AddWithValue("@parent_game_id", (object?)game.ParentGameId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", game.Title);
            cmd.Parameters.AddWithValue("@genre", ToDb(game.Genre));
            cmd.Parameters.AddWithValue("@platform", ToDb(game.Platform));
            cmd.Parameters.AddWithValue("@description", ToDb(game.Description));
            cmd.Parameters.AddWithValue("@release_year", game.ReleaseYear == 0 ? DBNull.Value : game.ReleaseYear);
            cmd.Parameters.AddWithValue("@status", game.Status.ToString());
            cmd.Parameters.AddWithValue("@size_gb", game.SizeGB == 0 ? DBNull.Value : game.SizeGB);
            cmd.Parameters.AddWithValue("@personal_rating", game.PersonalRating);
            cmd.Parameters.AddWithValue("@cover_image_path", ToDb(game.CoverImagePath));
            cmd.Parameters.AddWithValue("@executable_path", ToDb(game.ExecutablePath));
            cmd.Parameters.AddWithValue("@icon_path", ToDb(game.IconPath));
            cmd.Parameters.AddWithValue("@is_archived", game.IsArchived ? 1 : 0);
        }

        #endregion

        #region PlaySession

        public static List<PlaySession> GetSessions(IEnumerable<Game> games)
        {
            var gameById = games.ToDictionary(g => g.Id);
            var result = new List<PlaySession>();

            using var connection = OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT play_session_id, game_id, start_time, end_time, entry_method, note
                                 FROM PlaySession";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var session = new PlaySession
                {
                    Id = reader.GetInt32(0),
                    GameId = reader.GetInt32(1),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    EntryMethod = Enum.TryParse<EntryMethod>(reader.GetString(4), out var method) ? method : EntryMethod.Manual,
                    Note = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                };

                if (gameById.TryGetValue(session.GameId, out var game))
                    session.Game = game;

                result.Add(session);
            }
            return result;
        }

        public static void InsertSession(PlaySession session) =>
            Execute("Не вдалося зберегти сесію", () =>
            {
                if (session.Game != null)
                    session.GameId = session.Game.Id;

                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO PlaySession (game_id, start_time, end_time, entry_method, note)
                                 VALUES (@game_id, @start_time, @end_time, @entry_method, @note)";

                AddSessionParameters(cmd, session);
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
                cmd.CommandText = "SELECT last_insert_rowid()";
                session.Id = (int)(long)cmd.ExecuteScalar()!;
            });

        public static void UpdateSession(PlaySession session) =>
            Execute("Не вдалося оновити сесію", () =>
            {
                if (session.Game != null)
                    session.GameId = session.Game.Id;

                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE PlaySession
                                 SET game_id = @game_id, start_time = @start_time,
                                     end_time = @end_time, entry_method = @entry_method, note = @note
                                 WHERE play_session_id = @id";

                AddSessionParameters(cmd, session);
                cmd.Parameters.AddWithValue("@id", session.Id);
                cmd.ExecuteNonQuery();
            });

        public static void DeleteSession(int sessionId) =>
            Execute("Не вдалося видалити сесію", () =>
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM PlaySession WHERE play_session_id = @id";
                cmd.Parameters.AddWithValue("@id", sessionId);
                cmd.ExecuteNonQuery();
            });

        private static void AddSessionParameters(SqliteCommand cmd, PlaySession session)
        {
            cmd.Parameters.AddWithValue("@game_id", session.GameId);
            cmd.Parameters.AddWithValue("@start_time", session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end_time", session.EndTime.HasValue
                ? session.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : DBNull.Value);
            cmd.Parameters.AddWithValue("@entry_method", session.EntryMethod.ToString());
            cmd.Parameters.AddWithValue("@note", ToDb(session.Note));
        }

        #endregion
    }
}