using System.Text.RegularExpressions;

    namespace RouteCardProcess.Utilities
    {
        public static class InputSanitizer
        {
            public static string Sanitize(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return string.Empty;

                // Remove HTML tags and scripts
                var clean = Regex.Replace(input, "<.*?>", string.Empty);
                clean = Regex.Replace(clean, @"(script|on\w+)\s*=", "", RegexOptions.IgnoreCase);
                clean = clean.Replace("'", "").Replace("\"", "");
                return clean.Trim();
            }
        }
    }

