using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class NotWhiteSpaceAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string s && s.Length > 0 && string.IsNullOrWhiteSpace(s))
                return new ValidationResult(ErrorMessage ?? "Поле не може складатися лише з пробілів.");

            return ValidationResult.Success;
        }
    }
}