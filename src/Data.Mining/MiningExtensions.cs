using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Data.Mining
{
    public static class MiningExtensions
    {
        public static IEnumerable<string> GetPluralSynonyms(this string expression)
        {
            var synonyms = new List<string>(new[] { expression });

            if(!string.IsNullOrEmpty(expression))
            {
                synonyms = synonyms.Concat(new[]
                {
                    expression.Substring(0, expression.Length - 1).Trim(),
                    expression.Replace("Ä", "A").Replace("Ö", "O").Replace("Ü", "U").Replace("ä", "a").Replace("ö", "o").Replace("ü", "u").Trim()
                }).ToList();
            }

            return synonyms.Distinct();
        }
        public static IEnumerable<decimal> ParseNumbers(this string expression)
        {
            var numbers = default(IEnumerable<decimal>);

            var numberexpression = default(string);
            expression?.ToList().ForEach(character =>
            {
                if (new[] { '.', ',' }.Contains(character) || char.IsNumber(character))
                {
                    numberexpression = numberexpression ?? string.Empty;
                    numberexpression += character;
                }
                else
                {
                    if(!string.IsNullOrEmpty(numberexpression))
                    {
                        var @decimal = numberexpression.GetAsMatchingDecimal();
                        if(@decimal.HasValue)
                        {
                            numbers = numbers ?? Enumerable.Empty<decimal>();
                            numbers = numbers.Concat(new[] { @decimal.Value });
                        }

                        numberexpression = default(string);
                    }
                }
            });

            return numbers;
        }

        private static decimal? GetAsMatchingDecimal(this string value)
        {
            var @decimal = default(decimal?);

            if (!string.IsNullOrEmpty(value))
            {
                var number = value;
                var separators = @"(,|\.)";
                var matches = Regex.Matches(number, separators);
                var tokens = Regex.Split(number, separators).Where(s => !Regex.IsMatch(s, separators));
                if (matches != null && matches.Count > 1)
                {
                    number = string.Join(string.Empty, tokens.Take(tokens.Count() - 1));
                    number += "," + tokens.Last();
                }
                number = number.Replace(".", ",");
                tokens = number.Split(',');
                if (tokens.First().Length < 4 && tokens.Last().Length == 3)
                {
                    number = number.Replace(",", string.Empty);
                }

                var d = default(decimal);
                if (decimal.TryParse(
                    number,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("de-DE"),
                    out d
                ))
                {
                    @decimal = d;
                }
            }

            return @decimal;
        }
    }
}