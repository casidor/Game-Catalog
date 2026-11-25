namespace Game_Catalog.Services
{
    /// <summary> Ukrainian word forms that agree with a number. </summary>
    public static class Plural
    {
        /// <summary>
        /// Returns the number with the matching word form: Count(1, "ціль", "цілі", "цілей") gives "1 ціль",
        /// 3 gives "3 цілі", 5 and 11–14 give "5 цілей" and "12 цілей".
        /// </summary>
        /// <param name="one"> Form for 1, 21, 31… (but not 11). </param>
        /// <param name="few"> Form for 2–4, 22–24… (but not 12–14). </param>
        /// <param name="many"> Form for 0, 5–20, 25–30… </param>
        public static string Count(int count, string one, string few, string many)
        {
            var word = (count % 10, count % 100) switch
            {
                (1, not 11) => one,
                (>= 2 and <= 4, not (>= 12 and <= 14)) => few,
                _ => many
            };
            return $"{count} {word}";
        }
    }
}
