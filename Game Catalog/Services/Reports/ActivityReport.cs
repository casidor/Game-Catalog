using Game_Catalog.Converters;
using Game_Catalog.Models;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Services
{
    /// <summary> PDF report "Activity for a period": the finished sessions of the period, grouped by day. </summary>
    public static class ActivityReport
    {
        /// <summary> Title of the report, also used for the suggested file name. </summary>
        public const string Title = "Ігрова активність";

        private static readonly CultureInfo Ukrainian = CultureInfo.GetCultureInfo("uk-UA");

        private static readonly ReportColumn<ActivityReportRow>[] Columns =
        [
            // Fixed widths, in points, fit the longest values: "20:30", "999 г 59 хв", "Вручну".
            // The date is shown in the group rows, so a row needs only the start time.
            new("Гра", r => r.GameTitle, Bold: true),
            new("Початок", r => r.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture), Width: 64, Align: ReportAlign.Center),
            new("Тривалість", r => DurationText(r.DurationSeconds), Width: 84, Align: ReportAlign.Right),
            new("Спосіб", r => MethodText(r.EntryMethod), Width: 76, Align: ReportAlign.Center)
        ];

        private static readonly ReportGrouping<ActivityReportRow> ByDay = new(
            r => r.StartTime.Date,
            r => $"{r.StartTime:dd.MM.yyyy}, {Ukrainian.DateTimeFormat.GetDayName(r.StartTime.DayOfWeek)}",
            r => $"{Plural.Count(r.DaySessionCount, "сесія", "сесії", "сесій")} · {DurationText(r.DayTotalSeconds)}");

        /// <summary> Creates the report document for the period from its rows. </summary>
        /// <param name="rows"> Sessions of the period in chronological order, with the totals counted by the database. </param>
        /// <param name="from"> First day of the period. </param>
        /// <param name="to"> Last day of the period. </param>
        public static Document Create(IReadOnlyList<ActivityReportRow> rows, DateTime from, DateTime to)
        {
            // The period totals are the same in every row; with no rows there are no sessions.
            var sessionCount = rows.Count > 0 ? rows[0].PeriodSessionCount : 0;
            var totalSeconds = rows.Count > 0 ? rows[0].PeriodTotalSeconds : 0;

            ReportFact[] facts =
            [
                new("Період", $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}"),
                new("Сесій", sessionCount.ToString()),
                new("Загальний час", DurationText(totalSeconds))
            ];

            return ReportLayout.Create(Title, facts, landscape: false, Columns, rows, ByDay);
        }

        // The texts are taken from the converters of the UI, so the report reads the same as the app.
        private static string DurationText(int seconds) =>
            DurationConverter.Instance.Convert(seconds, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;

        private static string MethodText(EntryMethod method) =>
            EntryMethodConverter.Instance.Convert(method, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;
    }
}
