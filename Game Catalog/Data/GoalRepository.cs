using Dapper;
using Game_Catalog.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Data access for goals (read through the v_goal view). </summary>
    public static class GoalRepository
    {
        private const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// Returns goals of non-archived games matching the filter in the order chosen in it.
        /// Filtering and sorting are done by the database; goals without a due date go last when sorted by it.
        /// </summary>
        public static List<Goal> GetAll(GoalFilter filter)
        {
            // ORDER BY cannot be a query parameter, so the clause is chosen from fixed literals (no user text).
            var orderBy = (filter.SortField, filter.SortDescending) switch
            {
                (GoalSortField.DueDate, false) => "due_date ASC NULLS LAST, goal_id ASC",
                (GoalSortField.DueDate, true) => "due_date DESC NULLS LAST, goal_id ASC",
                (GoalSortField.CreatedAt, false) => "created_at ASC, goal_id ASC",
                (GoalSortField.CreatedAt, true) => "created_at DESC, goal_id ASC",
                (_, true) => "priority DESC, goal_id ASC",
                _ => "priority ASC, goal_id ASC"
            };

            return Db.Run("Не вдалося завантажити цілі", () =>
            {
                using var connection = Db.Open();
                return connection.Query<Goal>($@"
                    SELECT goal_id, game_id, game_title, goal_name, goal_description,
                           display_status AS status, priority, created_at, due_date, completed_at,
                           days_left, completion_days
                    FROM v_goal
                    WHERE is_archived = 0
                      AND (@GameCount = 0 OR game_id IN @GameIds)
                      AND (@PriorityCount = 0 OR priority IN @Priorities)
                      AND (@StatusCount = 0 OR display_status IN @Statuses)
                      AND (@DueFrom IS NULL OR due_date >= @DueFrom)
                      AND (@DueTo IS NULL OR due_date <= @DueTo)
                    ORDER BY {orderBy}",
                    ToFilterParams(filter)).AsList();
            });
        }

        /// <summary>
        /// Returns ids of the goals whose name or description contains the search text, ignoring case.
        /// The match is decided by the database; an empty text matches nothing.
        /// </summary>
        public static HashSet<int> GetMatchIds(string? search)
        {
            var text = search?.Trim() ?? string.Empty;
            if (text.Length == 0) return new HashSet<int>();

            return Db.Run("Не вдалося виконати пошук цілей", () =>
            {
                using var connection = Db.Open();
                return connection.Query<int>(@"
                    SELECT goal_id
                    FROM Goal
                    WHERE instr(lower_ua(goal_name), lower_ua(@Search)) > 0
                       OR instr(lower_ua(goal_description), lower_ua(@Search)) > 0",
                    new { Search = text }).ToHashSet();
            });
        }

        /// <summary> Inserts the goal as active and sets its generated id. The creation time is taken by the database. </summary>
        public static void Insert(Goal goal) =>
            Db.Run("Не вдалося зберегти ціль", () =>
            {
                using var connection = Db.Open();
                goal.GoalId = connection.ExecuteScalar<int>(@"
                    INSERT INTO Goal (game_id, goal_name, goal_description, priority, due_date)
                    VALUES (@GameId, trim(@GoalName), NULLIF(trim(@GoalDescription), ''), @Priority, @DueDate);
                    SELECT last_insert_rowid();", ToParams(goal));
            });

        /// <summary> Updates the game, name, description, priority and due date of the goal. The status is not touched. </summary>
        public static void Update(Goal goal) =>
            Db.Run("Не вдалося оновити ціль", () =>
            {
                using var connection = Db.Open();
                connection.Execute(@"
                    UPDATE Goal
                    SET game_id = @GameId,
                        goal_name = trim(@GoalName),
                        goal_description = NULLIF(trim(@GoalDescription), ''),
                        priority = @Priority,
                        due_date = @DueDate
                    WHERE goal_id = @GoalId", ToParams(goal));
            });

        /// <summary>
        /// Marks the goals as completed now, all or none. Only active goals change (overdue ones included);
        /// the completion time is taken by the database.
        /// </summary>
        public static void Complete(IReadOnlyCollection<int> goalIds) =>
            Db.InTransaction("Не вдалося позначити цілі виконаними", (connection, tx) =>
            {
                foreach (var goalId in goalIds)
                    connection.Execute(@"
                        UPDATE Goal
                        SET goal_status = 'Completed',
                            completed_at = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
                        WHERE goal_id = @goalId AND goal_status = 'Active'",
                        new { goalId }, tx);
            });

        /// <summary> Cancels the goals, all or none. Only active goals change (overdue ones included). </summary>
        public static void Cancel(IReadOnlyCollection<int> goalIds) =>
            Db.InTransaction("Не вдалося скасувати цілі", (connection, tx) =>
            {
                foreach (var goalId in goalIds)
                    connection.Execute(@"
                        UPDATE Goal
                        SET goal_status = 'Cancelled'
                        WHERE goal_id = @goalId AND goal_status = 'Active'",
                        new { goalId }, tx);
            });

        /// <summary> Returns completed and cancelled goals to active, all or none, clearing the completion time. </summary>
        public static void Reopen(IReadOnlyCollection<int> goalIds) =>
            Db.InTransaction("Не вдалося повернути цілі в активні", (connection, tx) =>
            {
                foreach (var goalId in goalIds)
                    connection.Execute(@"
                        UPDATE Goal
                        SET goal_status = 'Active',
                            completed_at = NULL
                        WHERE goal_id = @goalId AND goal_status <> 'Active'",
                        new { goalId }, tx);
            });

        /// <summary> Deletes the goals, all or none. </summary>
        public static void Delete(IReadOnlyCollection<int> goalIds) =>
            Db.InTransaction("Не вдалося видалити цілі", (connection, tx) =>
            {
                foreach (var goalId in goalIds)
                    connection.Execute("DELETE FROM Goal WHERE goal_id = @goalId", new { goalId }, tx);
            });

        /// <summary> Filter parameters. Statuses are passed by name, due date bounds as plain dates. </summary>
        private static object ToFilterParams(GoalFilter filter) => new
        {
            GameCount = filter.GameIds.Count,
            GameIds = filter.GameIds.ToArray(),
            PriorityCount = filter.Priorities.Count,
            Priorities = filter.Priorities.ToArray(),
            StatusCount = filter.Statuses.Count,
            Statuses = filter.Statuses.Select(s => s.ToString()).ToArray(),
            DueFrom = filter.DueFrom?.ToString(DateFormat, CultureInfo.InvariantCulture),
            DueTo = filter.DueTo?.ToString(DateFormat, CultureInfo.InvariantCulture)
        };

        /// <summary> Goal parameters. The due date is written as a plain date, as the table requires. </summary>
        private static object ToParams(Goal goal) => new
        {
            goal.GoalId,
            goal.GameId,
            goal.GoalName,
            goal.GoalDescription,
            goal.Priority,
            DueDate = goal.DueDate?.ToString(DateFormat, CultureInfo.InvariantCulture)
        };
    }
}