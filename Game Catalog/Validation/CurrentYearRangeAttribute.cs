using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CurrentYearRangeAttribute : ValidationAttribute
    {
        public int MinYear { get; }

        public CurrentYearRangeAttribute(int minYear)
        {
            MinYear = minYear;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not int year)
                return new ValidationResult("Некоректний тип року.");

            int currentYear = DateTime.Now.Year;
            if (year < MinYear || year > currentYear)
                return new ValidationResult(ErrorMessage ?? $"Рік має бути між {MinYear} і {currentYear}");

            return ValidationResult.Success;
        }
    }
}