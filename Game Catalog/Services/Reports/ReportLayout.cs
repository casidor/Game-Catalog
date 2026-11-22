using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Game_Catalog.Services
{
    /// <summary> Common page layout and table styles shared by all PDF reports. </summary>
    public static class ReportLayout
    {
        /// <summary> Text shown in a cell when the value is missing. </summary>
        public const string Missing = "–";

        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

        /// <summary> Creates an A4 report with the title, the generation time, page numbers and the given content. </summary>
        /// <param name="title"> Report title shown in the header and stored in the PDF metadata. </param>
        /// <param name="parameters"> Line with the report parameters, such as the period, or null if there are none. </param>
        /// <param name="landscape"> True for wide tables that do not fit a portrait page. </param>
        /// <param name="content"> Draws the report body, usually a table. </param>
        public static Document Create(string title, string? parameters, bool landscape, Action<IContainer> content) =>
            Document.Create(document => document.Page(page =>
                {
                    page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Element(header => ComposeHeader(header, title, parameters));
                    page.Content().PaddingVertical(10).Element(content);
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Сторінка ");
                        text.CurrentPageNumber();
                        text.Span(" з ");
                        text.TotalPages();
                    });
                }))
                .WithMetadata(new DocumentMetadata { Title = title });

        /// <summary> Generates the PDF file in the background, replacing the file if it exists. </summary>
        public static Task SaveAsync(Document document, string path) =>
            Task.Run(() => document.GeneratePdf(path));

        /// <summary> Style of a table header cell. The header is repeated on every page. </summary>
        public static IContainer HeaderCell(this IContainer container) =>
            container.Background(Colors.Grey.Lighten3)
                .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
                .PaddingVertical(4).PaddingHorizontal(4)
                .DefaultTextStyle(style => style.SemiBold());

        /// <summary> Style of a table body cell. </summary>
        public static IContainer BodyCell(this IContainer container) =>
            container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(3).PaddingHorizontal(4);

        private static void ComposeHeader(IContainer container, string title, string? parameters)
        {
            container.Column(column =>
            {
                column.Item().Text(title).FontSize(16).SemiBold();
                if (parameters != null)
                    column.Item().Text(parameters);
                column.Item().Text($"Сформовано: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Culture)}")
                    .FontColor(Colors.Grey.Darken1);
            });
        }
    }
}
