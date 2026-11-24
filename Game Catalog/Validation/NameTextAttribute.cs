using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.Validation
{
    /// <summary>
    /// Checks that a name has at least one letter or digit; any other characters, emoji included, are allowed.
    /// An empty value passes, so an optional name can stay empty; a required one also needs <see cref="RequiredAttribute"/>.
    /// The name is expected to be normalized with <see cref="TextRules.NormalizeName"/> before saving.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class NameTextAttribute : ValidationAttribute
    {
        /// <summary> Name of the field in the message, such as "Назва" or "Назва виробника". </summary>
        public string Field { get; }

        public NameTextAttribute(string field = "Назва")
        {
            Field = field;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string text && text.Length > 0 && !TextRules.HasLetterOrDigit(text))
                return new ValidationResult($"{Field} має містити хоча б одну літеру або цифру");

            return ValidationResult.Success;
        }
    }
}
