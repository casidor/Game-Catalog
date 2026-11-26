using Dapper;
using System;
using System.IO;

namespace Game_Catalog.Data
{
    /// <summary> Fills an empty library with the demo data from seed.sql. Used from the developer query editor. </summary>
    public static class DemoData
    {
        /// <summary>
        /// Runs seed.sql in one transaction, so the data is added whole or not at all.
        /// Refuses when the library already has games, so the demo data never mixes with real data.
        /// </summary>
        public static void Fill() =>
            Db.InTransaction("Не вдалося заповнити демо-даними", (connection, tx) =>
            {
                var hasGames = connection.ExecuteScalar<bool>("SELECT EXISTS (SELECT 1 FROM Game)", transaction: tx);
                if (hasGames)
                    throw new DatabaseException(DatabaseErrorKind.Constraint,
                        "У бібліотеці вже є ігри. Демо-дані додаються лише до порожньої бази.");

                var path = Path.Combine(AppContext.BaseDirectory, "Data", "seed.sql");
                if (!File.Exists(path))
                    throw new DatabaseException(DatabaseErrorKind.Unavailable,
                        $"Файл демо-даних не знайдено: {path}");

                connection.Execute(File.ReadAllText(path), transaction: tx);
            });
    }
}
