using System.Linq;
using System.Text;

namespace Game_Catalog.Validation
{
    /// <summary>
    /// Cleaning of text typed by the user before it is saved.
    /// Names are trimmed and their inner whitespace is collapsed, so " Hades  II " and "Hades II" are the same name;
    /// free text such as a description is only trimmed.
    /// </summary>
    public static class TextRules
    {
        /// <summary> Trims a name and collapses any run of whitespace inside it, line breaks included, to one space. </summary>
        public static string NormalizeName(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var result = new StringBuilder(text.Length);
            var pendingSpace = false;

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = result.Length > 0;
                    continue;
                }
                if (pendingSpace)
                {
                    result.Append(' ');
                    pendingSpace = false;
                }
                result.Append(c);
            }
            return result.ToString();
        }

        /// <summary> Trims free text and unifies its line breaks; the text inside is kept as typed. </summary>
        public static string NormalizeFree(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

        /// <summary> True when the text has at least one letter or digit, so a name such as "---" is rejected. </summary>
        public static bool HasLetterOrDigit(string text) =>
            text.Any(char.IsLetterOrDigit);
    }
}
