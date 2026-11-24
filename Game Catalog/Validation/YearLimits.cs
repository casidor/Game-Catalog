using System;

namespace Game_Catalog.Validation
{
    /// <summary>
    /// Allowed years of games and studios, in one place for the validation and the input fields.
    /// The lower bounds match the CHECK constraints of the schema; the upper bounds follow the current year:
    /// a game may be announced a few years ahead, a studio cannot be founded in the future.
    /// </summary>
    public static class YearLimits
    {
        /// <summary> Earliest release year of a game. </summary>
        public const int MinReleaseYear = 1900;

        /// <summary> How many years ahead of the current one a release year may be, for announced games. </summary>
        public const int ReleaseYearsAhead = 5;

        /// <summary> Earliest foundation year of a studio. </summary>
        public const int MinFoundationYear = 1800;

        /// <summary> The latest allowed year: the current year plus the given number of years ahead. </summary>
        public static int MaxYear(int yearsAhead = 0) => DateTime.Today.Year + yearsAhead;

        // NumericUpDown takes decimal bounds, so the input fields read these properties.

        /// <summary> Lowest value of a release year field. </summary>
        public static decimal ReleaseYearMinimum => MinReleaseYear;

        /// <summary> Highest value of a release year field: the current year plus <see cref="ReleaseYearsAhead"/>. </summary>
        public static decimal ReleaseYearMaximum => MaxYear(ReleaseYearsAhead);

        /// <summary> Lowest value of a foundation year field. </summary>
        public static decimal FoundationYearMinimum => MinFoundationYear;

        /// <summary> Highest value of a foundation year field: the current year. </summary>
        public static decimal FoundationYearMaximum => MaxYear();
    }
}
