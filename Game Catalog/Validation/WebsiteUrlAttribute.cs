using System;
using System.ComponentModel.DataAnnotations;

namespace Game_Catalog.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class WebsiteUrlAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s))
                return ValidationResult.Success;

            var url = s.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && uri.Host.Contains('.')
                && !uri.Host.StartsWith('.') && !uri.Host.EndsWith('.'))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? "Вкажіть коректну адресу сайту, наприклад example.com");
        }
    }
}