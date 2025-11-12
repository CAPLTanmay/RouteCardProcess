using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Middleware
{
    public enum SafeTextPattern
    {
        AlphaNumeric,
        AlphaOnly,
        NumericOnly,
        AlphaNumericWithSymbols,
         UnicodeText
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SafeTextAttribute : ValidationAttribute
    {
        private readonly SafeTextPattern _pattern;
        public int MaxLength { get; }
        public bool AllowEmpty { get; set; } = false;
        public SafeTextAttribute(SafeTextPattern pattern = SafeTextPattern.AlphaNumeric, int maxLength = 100)
        {
            _pattern = pattern;
            MaxLength = maxLength;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var input = value.ToString();
            if (input.Length > MaxLength)
                return new ValidationResult($"Maximum {MaxLength} characters allowed.");

            string regex = _pattern switch
            {
                SafeTextPattern.AlphaOnly => @"^[a-zA-Z\s]+$",
                SafeTextPattern.NumericOnly => @"^[0-9]+$",
                SafeTextPattern.AlphaNumericWithSymbols => @"^[a-zA-Z0-9\s\-\.,_@]+$",
                //SafeTextPattern.UnicodeText => @"^[\p{L}\p{N}\p{Zs}\p{P}]+$",
                SafeTextPattern.UnicodeText => @"^[\p{IsDevanagari}\p{L}\p{N}\p{Zs}\p{P}]+$",

                _ => @"^[a-zA-Z0-9\s\-\.,]+$"
            };

            if (!System.Text.RegularExpressions.Regex.IsMatch(input, regex))
                return new ValidationResult("Invalid characters detected.");

            return ValidationResult.Success;
        }
    }

}
