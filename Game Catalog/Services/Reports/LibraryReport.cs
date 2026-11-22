using Game_Catalog.Converters;
using Game_Catalog.Models;
using QuestPDF.Fluent;
using System.Collections.Generic;
using System.Globalization;

namespace Game_Catalog.Services
{
    /// <summary> PDF report "Game library": every standalone game that is not archived. </summary>
    public static class LibraryReport
    {
        /// <summary> Title of the report, also used for the suggested file name. </summary>
        public const string Title = "Бібліотека ігор";

        private static readonly ReportColumn<LibraryReportRow>[] Columns =
        [
            // Fixed widths, in points, fit the longest values: "Пройдено", "10 / 10", "999 г 59 хв", "999,9 ГБ".
            // The text columns share the rest; the title gets the most room, so most titles fit one or two lines.
            new("Назва", r => r.GameTitle, Weight: 3.5f, Bold: true),
            new("Студія", r => r.DeveloperName, Weight: 2),
            new("Жанри", r => r.Genres ?? ReportLayout.Missing, Weight: 2),
            new("Платформи", r => r.Platforms ?? ReportLayout.Missing, Weight: 2),
            new("Рік", r => r.ReleaseYear?.ToString() ?? ReportLayout.Missing, Width: 44, Align: ReportAlign.Center),
            new("Статус", r => StatusText(r.GameStatus), Width: 84, Align: ReportAlign.Center, Badge: true),
            new("Оцінка", r => r.PersonalRating is { } rating ? $"{rating} / 10" : ReportLayout.Missing, Width: 60, Align: ReportAlign.Center),
            new("Час гри", r => DurationText(r.TotalHours), Width: 76, Align: ReportAlign.Right),
            new("Обсяг", r => SizeText(r.SizeGb), Width: 70, Align: ReportAlign.Right)
        ];

        /// <summary> Creates the report document from the rows and the totals of the library. </summary>
        public static Document Create(IReadOnlyList<LibraryReportRow> rows, LibrarySummary summary)
        {
            ReportFact[] facts =
            [
                new("Ігор", summary.GameCount.ToString()),
                new("Загальний час", DurationText(summary.TotalHours)),
                new("Обсяг на диску", SizeText(summary.TotalSizeGb))
            ];

            return ReportLayout.Create(Title, facts, landscape: true, Columns, rows);
        }

        private static string SizeText(double? sizeGb) =>
            sizeGb is { } s ? $"{s:0.0} ГБ" : ReportLayout.Missing;

        // The texts are taken from the converters of the UI, so the report reads the same as the app.
        private static string StatusText(GameStatus status) =>
            GameStatusConverter.Instance.Convert(status, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;

        private static string DurationText(double hours) =>
            DurationConverter.Instance.Convert(hours, typeof(string), null, CultureInfo.CurrentCulture) as string
            ?? ReportLayout.Missing;
    }
}
