using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Game_Catalog.Services
{
    /// <summary> Horizontal alignment of a report column. </summary>
    public enum ReportAlign
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Column of a report table: the header and how to get the cell text from a row.
    /// A column with a <see cref="Width"/> in points has a fixed size, for short values such as numbers,
    /// dates and statuses. A column without it shares the rest of the table width with the other such
    /// columns in proportion to <see cref="Weight"/>, and its text wraps, for titles and lists.
    /// <see cref="Bold"/> is for the column that names the row, <see cref="Badge"/> for statuses.
    /// </summary>
    public sealed record ReportColumn<T>(string Header, Func<T, string> Value,
        float? Width = null, float Weight = 1, ReportAlign Align = ReportAlign.Left, bool Bold = false, bool Badge = false);

    /// <summary>
    /// Grouping of report rows: rows with the same <see cref="Key"/> follow each other
    /// under a full-width row with the <see cref="Title"/>, such as the goals of one game.
    /// An optional <see cref="Summary"/> is shown muted on the right of the title row, such as the totals of a day;
    /// both are taken from the first row of the group. The rows must already be ordered by the key.
    /// </summary>
    public sealed record ReportGrouping<T>(Func<T, object> Key, Func<T, string> Title, Func<T, string>? Summary = null);

    /// <summary> Summary value shown as a card on the first page of a report, such as a total or the period. </summary>
    public sealed record ReportFact(string Label, string Value);

    /// <summary> Common page layout and table style shared by all PDF reports. </summary>
    public static class ReportLayout
    {
        /// <summary> Text shown in a cell when the value is missing. </summary>
        public const string Missing = "–";

        private const string AppName = "Бібліотека відеоігор";

        private const string TextColor = "#0F172A";
        private const string MutedColor = "#64748B";
        private const string MissingColor = "#94A3B8";
        private const string AccentColor = "#5A64A8";
        private const string LineColor = "#CBD5E1";
        private const string CardBackground = "#F8FAFC";
        private const string HeaderBackground = "#F1F5F9";
        private const string HeaderTextColor = "#475569";
        private const string RowBackground = "#FFFFFF";
        private const string StripeBackground = "#F8FAFC";
        private const string BadgeBackground = "#E2E8F0";
        private const string GroupBackground = "#E2E8F0";

        private const float PageMargin = 42.5f;   // 1.5 cm in points
        private const float TableBorder = 0.5f;
        private const float CellPadding = 8f;     // horizontal padding of every cell
        private const float BadgePadding = 8f;    // horizontal padding inside a badge

        // Room a group needs on a page before it may start there: its title row and about one row.
        // With less room left, a group that does not fit the page starts on the next one.
        private const float GroupKeepSpace = 60f;

        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

        /// <summary>
        /// Creates an A4 report. The title and the summary cards are shown on the first page only;
        /// the footer with the app name and the page number is on every page.
        /// </summary>
        /// <param name="title"> Report title, also stored in the PDF metadata. </param>
        /// <param name="facts"> Summary values shown as cards under the title, such as totals or the period. </param>
        /// <param name="landscape"> True for wide tables that do not fit a portrait page. </param>
        /// <param name="columns"> Columns of the report table. </param>
        /// <param name="rows"> Rows of the report table, in the order they are printed. </param>
        /// <param name="grouping"> Groups the rows under title rows, or null for a plain table. </param>
        public static Document Create<T>(string title, IReadOnlyList<ReportFact> facts, bool landscape,
            IReadOnlyList<ReportColumn<T>> columns, IReadOnlyList<T> rows, ReportGrouping<T>? grouping = null)
        {
            var size = landscape ? PageSizes.A4.Landscape() : PageSizes.A4;

            return Document.Create(document => document.Page(page =>
                {
                    page.Size(size);
                    page.Margin(PageMargin);
                    page.DefaultTextStyle(style => style.FontSize(10).FontColor(TextColor));

                    page.Content().Column(column =>
                    {
                        column.Item().Element(intro => ComposeIntro(intro, title, facts));
                        column.Item().PaddingTop(16).Element(table => ComposeTable(table, columns, rows, grouping));
                    });
                    page.Footer().PaddingTop(8).Element(ComposeFooter);
                }))
                .WithMetadata(new DocumentMetadata { Title = title });
        }

        /// <summary> Generates the PDF file in the background, replacing the file if it exists. </summary>
        public static Task SaveAsync(Document document, string path) =>
            Task.Run(() => document.GeneratePdf(path));

        /// <summary>
        /// Draws a table with a header repeated on every page, an outer border
        /// and striped rows. Missing values are muted.
        /// Every cell has the same padding. Text wraps between words; QuestPDF itself splits a word
        /// that is wider than the whole column, so a long title without spaces does not break the layout.
        /// </summary>
        private static void ComposeTable<T>(IContainer container, IReadOnlyList<ReportColumn<T>> columns,
            IReadOnlyList<T> rows, ReportGrouping<T>? grouping)
        {
            var cells = rows.Select(row => columns.Select(column => column.Value(row)).ToArray()).ToList();

            container.Border(TableBorder).BorderColor(LineColor).Table(table =>
            {
                DefineColumns(table, columns);

                table.Header(header =>
                {
                    foreach (var column in columns)
                        Align(HeaderCell(header.Cell()), column.Align).Text(column.Header);
                });

                if (grouping == null)
                {
                    ComposeRows(table, columns, cells, 0, cells.Count);
                    return;
                }

                // Every group is one cell across the whole table with its own inner table:
                // the title row and the rows of the group, with the same columns as the outer table.
                // EnsureSpace works on the whole group: if the group has to break across pages
                // and there is too little room left for its title and first rows, it starts on the next page.
                var start = 0;
                while (start < cells.Count)
                {
                    var key = grouping.Key(rows[start]);
                    var end = start + 1;
                    while (end < cells.Count && Equals(grouping.Key(rows[end]), key))
                        end++;

                    var title = grouping.Title(rows[start]);
                    var summary = grouping.Summary?.Invoke(rows[start]);
                    var from = start;
                    var to = end;
                    table.Cell().ColumnSpan((uint)columns.Count)
                        .EnsureSpace(GroupKeepSpace)
                        .Table(group =>
                        {
                            DefineColumns(group, columns);
                            group.Cell().ColumnSpan((uint)columns.Count)
                                .Background(GroupBackground)
                                .PaddingVertical(6).PaddingHorizontal(CellPadding)
                                .Row(row =>
                                {
                                    row.RelativeItem().Text(title).SemiBold();
                                    if (summary != null)
                                        row.AutoItem().Text(summary).FontColor(MutedColor);
                                });
                            ComposeRows(group, columns, cells, from, to);
                        });

                    start = end;
                }
            });
        }

        /// <summary> Fixed columns get their width in points, the others share the rest by weight. </summary>
        private static void DefineColumns<T>(TableDescriptor table, IReadOnlyList<ReportColumn<T>> columns)
        {
            table.ColumnsDefinition(definition =>
            {
                foreach (var column in columns)
                {
                    if (column.Width is { } width)
                        definition.ConstantColumn(width);
                    else
                        definition.RelativeColumn(column.Weight);
                }
            });
        }

        /// <summary> Draws the rows from <paramref name="from"/> up to <paramref name="to"/>, not included, as zebra stripes. </summary>
        private static void ComposeRows<T>(TableDescriptor table, IReadOnlyList<ReportColumn<T>> columns,
            List<string[]> cells, int from, int to)
        {
            for (var r = from; r < to; r++)
            {
                // Zebra: every second row (odd index, counting from the first row) gets the stripe colour.
                var background = (r - from) % 2 == 1 ? StripeBackground : RowBackground;
                for (var c = 0; c < columns.Count; c++)
                    ComposeCell(BodyCell(table.Cell().Background(background)), columns[c], cells[r][c]);
            }
        }

        private static void ComposeCell<T>(IContainer container, ReportColumn<T> column, string value)
        {
            if (value == Missing)
            {
                Align(container, column.Align).Text(value).FontColor(MissingColor);
                return;
            }

            if (column.Badge)
            {
                // The badge takes the full column width, so all badges are the same size,
                // and only the height of its text, so it does not stretch to a tall row.
                container.ShrinkVertical()
                    .Background(BadgeBackground).CornerRadius(8)
                    .PaddingHorizontal(BadgePadding).PaddingVertical(1)
                    .AlignCenter().Text(value);
                return;
            }

            var text = Align(container, column.Align).Text(value);
            if (column.Bold)
                text.SemiBold();
        }

        private static IContainer HeaderCell(IContainer container) =>
            container.Background(HeaderBackground)
                .BorderBottom(1).BorderColor(LineColor)
                .PaddingVertical(6).PaddingHorizontal(CellPadding)
                .DefaultTextStyle(style => style.SemiBold().FontColor(HeaderTextColor));

        private static IContainer BodyCell(IContainer container) =>
            container.PaddingVertical(6).PaddingHorizontal(CellPadding);

        private static IContainer Align(IContainer container, ReportAlign align) => align switch
        {
            ReportAlign.Center => container.AlignCenter(),
            ReportAlign.Right => container.AlignRight(),
            _ => container.AlignLeft()
        };

        /// <summary> Title with an accent bar and the generation time, then a row of summary cards. </summary>
        private static void ComposeIntro(IContainer container, string title, IReadOnlyList<ReportFact> facts)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(4).Background(AccentColor);
                    row.ConstantItem(12);
                    row.RelativeItem().AlignMiddle().Text(title).FontSize(24).Bold();
                    row.AutoItem().AlignMiddle()
                        .Text($"Сформовано: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Culture)}")
                        .FontColor(MutedColor);
                });

                if (facts.Count == 0) return;

                column.Item().PaddingTop(16).Row(row =>
                {
                    row.Spacing(12);
                    foreach (var fact in facts)
                    {
                        row.RelativeItem()
                            .Border(0.5f).BorderColor(LineColor).CornerRadius(6).Background(CardBackground)
                            .PaddingVertical(10).PaddingHorizontal(12)
                            // Cards in a row share the height of the tallest one, so a short text is centred vertically too.
                            .AlignCenter().AlignMiddle()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(style => style.FontSize(12));
                                text.Span($"{fact.Label}: ");
                                text.Span(fact.Value).Bold();
                            });
                    }
                });
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.BorderTop(0.5f).BorderColor(LineColor).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(AppName).FontColor(MutedColor);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontColor(MutedColor));
                    text.Span("Сторінка ");
                    text.CurrentPageNumber();
                    text.Span(" з ");
                    text.TotalPages();
                });
            });
        }
    }
}
