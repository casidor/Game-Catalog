using Game_Catalog.Converters;
using Game_Catalog.Models;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Services
{
    /// <summary> PDF report "Player goals": every goal of the games that are not archived, grouped by game. </summary>
    public static class GoalReport
    {
        /// <summary> Title of the report, also used for the suggested file name. </summary>
        public const string Title = "Цілі гравця";

        private static readonly ReportColumn<GoalReportRow>[] Columns =
        [
            // Fixed widths, in points, fit the longest values: "Найнижчий", "22.11.2026", "Невиконана".
            // The goal and its description share the rest; the game is shown in the group rows.
            new("Ціль", r => r.GoalName, Weight: 2.5f, Bold: true),
            new("Опис", r => r.GoalDescription ?? ReportLayout.Missing, Weight: 3.5f),
            new("Пріоритет", r => PriorityText(r.Priority), Width: 74, Align: ReportAlign.Center),
            new("Створено", r => DateText(r.CreatedAt), Width: 74, Align: ReportAlign.Center),
            new("Термін", r => DateText(r.DueDate), Width: 74, Align: ReportAlign.Center),
            new("Статус", r => StatusText(r.DisplayStatus), Width: 92, Align: ReportAlign.Center, Badge: true),
            new("Виконано", r => DateText(r.CompletedAt), Width: 74, Align: ReportAlign.Center),
            new("Залишок днів", r => r.DaysLeft?.ToString() ?? ReportLayout.Missing, Width: 64, Align: ReportAlign.Right)
        ];

        private static readonly ReportGrouping<GoalReportRow> ByGame = new(r => r.GameId, r => r.GameTitle);

        /// <summary> Creates the report document from the rows and the goal counts. </summary>
        public static Document Create(IReadOnlyList<GoalReportRow> rows, GoalSummary summary)
        {
            ReportFact[] facts =
            [
                new("Цілей", summary.TotalCount.ToString()),
                new("Активних", summary.ActiveCount.ToString()),
                new("Невиконаних", summary.OverdueCount.ToString()),
                new("Виконаних", summary.CompletedCount.ToString()),
                new("Скасованих", summary.CancelledCount.ToString())
            ];

            return ReportLayout.Create(Title, facts, landscape: true, Columns, rows, ByGame);
        }

        private static string DateText(DateTime? date) =>
            date?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? ReportLayout.Missing;

        // The texts are taken from the converters of the UI, so the report reads the same as the app.
        private static string PriorityText(int priority) =>
            GoalPriorityConverter.Instance.Convert(priority, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;

        private static string StatusText(GoalStatus status) =>
            GoalStatusConverter.Instance.Convert(status, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;
    }
}
