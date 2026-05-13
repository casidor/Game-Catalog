using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CurrentYearRangeAttribute : ValidationAttribute
    {
        public int MinYear { get; }
        public int MaxYearOffset { get; }

        public CurrentYearRangeAttribute(int minYear, int maxYearOffset = 0)
        {
            MinYear = minYear;
            MaxYearOffset = maxYearOffset;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not int year)
                return new ValidationResult("Вкажіть коректний рік");

            int maxYear = DateTime.Now.Year + MaxYearOffset;
            if (year < MinYear || year > maxYear)
                return new ValidationResult(ErrorMessage ?? $"Рік має бути між {MinYear} і {maxYear}");

            return ValidationResult.Success;
        }
    }
}