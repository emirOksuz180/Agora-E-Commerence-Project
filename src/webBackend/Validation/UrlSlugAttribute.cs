using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace webBackend.Validation
{
    public class UrlSlugAttribute : ValidationAttribute
    {
        private readonly Regex _regex = new(@"^[a-z0-9-]+$");

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (!_regex.IsMatch(value.ToString()!))
            {
                return new ValidationResult("URL formatı geçersiz.Lütfen sadece küçük harf, rakam ve tire kullanın");
            }

            return ValidationResult.Success;
        }
    }
}