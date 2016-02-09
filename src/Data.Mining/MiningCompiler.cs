using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Data.Mining
{
    public class MiningCompiler
    {
        internal static IEnumerable<string> Expressionscurrency = new[]
        {
            "€",
            "$",
            "dollar",
            "eur",
            "euro"
        };
        internal static IEnumerable<string> Separatorsrangefrom = new[]
        {
            "ab", "von", "zwischen"
        };
        internal static IEnumerable<string> Separatorsrangeto = new[]
        {
            "bis"
        };
        internal static IEnumerable<string> Separatorsrange = 
            Separatorsrangefrom.Concat(Separatorsrangeto).Concat(new[] 
            {
                "-", "/"
            })
        ;
        internal static IEnumerable<string> Separatorsselection = new[] { ",", "oder", "und" };
        internal static IEnumerable<string> Separators = Separatorsrange.Concat(Separatorsselection);

        public static MiningFilter ComposeFilter(string question)
        {
            var filter = default(MiningFilter);

            question = question?.ToLower();

            var filters = new List<MiningFilter>();

            //  Farbe
            filters.Add(ComposeFilterColor(question));
            //  Größe
            filters.Add(ComposeFilterSize(question));
            //  Preis
            filters.Add(ComposeFilterPrice(question));

            filters = filters.Where(f => f != null).ToList();

            //  Aggrigieren der gesammelten Filter
            if (filters != null && filters.Any())
            {
                filter = new MiningFilter { };
                filter.And = filters;
            }

            return filter;
        }

        private static MiningFilter ComposeFilterColor(string question)
        {
            var filter = default(MiningFilter);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Data.Mining.colors.json"))
            { 
                using (var reader = new StreamReader(stream))
                {
                    var colorfilters = Enumerable.Empty<MiningFilter>();

                    var jsoncolors = JsonConvert.DeserializeObject(reader.ReadToEnd()) as JObject;
                    var jcolors = jsoncolors.SelectTokens("$.colors[?(@.description != '')]");
                    foreach(var jcolor in jcolors)
                    {
                        var color = jcolor.SelectToken("description").ToString();
                        var subcolorids = jcolor.SelectToken("subcolors") as JArray;
                        var synonyms = new[] { color, $"{color}e" };

                        if(synonyms.Any(
                            synonym => 
                                question.ToLower().StartsWith(synonym) ||
                                Regex.IsMatch(question.ToLower(), $"( |,){synonym}( |,)")
                        ))
                        {
                            colorfilters = colorfilters.Concat(new[] { new MiningFilter {
                                Target = "color",
                                Value = $".*?{color}.*?"
                            } });

                            //  Farbalternativen durch Subfarben
                            if (subcolorids != null && subcolorids.Any())
                            {
                                subcolorids.AsParallel().ForAll(colorid =>
                                {
                                    var jsubcolor = jsoncolors.SelectToken($"$.colors[?(@.id == {colorid})]");
                                    var subcolor = jsubcolor?.SelectToken("description").ToString();

                                    colorfilters = colorfilters.Concat(new[] { new MiningFilter {
                                        Target = "color",
                                        Value = $".*?{subcolor}.*?"
                                    } });
                                });
                            }
                        }
                    }

                    if(colorfilters.Any())
                    {
                        filter = new MiningFilter
                        {
                            Or = colorfilters
                        };
                    }
                }
            }

            return filter;
        }
        private static MiningFilter ComposeFilterPrice(string question)
        {
            var filter = default(MiningFilter);

            var expression = string.Empty;
            var regularexpression = string.Empty;
            var options = RegexOptions.RightToLeft;

            //  Bereich Spanne
            regularexpression = $@"({string.Join("|", Separatorsrangefrom)}).*({string.Join("|", Separatorsrangeto)}).*({string.Join("|", Expressionscurrency)})";
            expression = Regex.Match(question, regularexpression, options)?.Value.Trim();
            //  Bereich von oder bis
            if(string.IsNullOrEmpty(expression))
            {
                regularexpression = $@"({string.Join("|", Separatorsrange)}).*?({string.Join("|", Expressionscurrency)})";
                expression = Regex.Match(question, regularexpression, options)?.Value.Trim();
            }

            var numbers = expression.ParseNumbers();
            var from = default(decimal?);
            var to = default(decimal?);

            if(numbers != null)
            {
                if(numbers.Count() == 2)
                {
                    from = numbers.First();
                    to = numbers.Last();
                }
                else
                {
                    regularexpression = $@"({string.Join("|", Separatorsrangefrom)}).*({string.Join("|", Expressionscurrency)})";
                    from = Regex.IsMatch(expression, regularexpression) ? numbers.First() : from;

                    regularexpression = $@"({string.Join("|", Separatorsrangeto)}).*({string.Join("|", Expressionscurrency)})";
                    to = Regex.IsMatch(expression, regularexpression) ? numbers.First() : to;
                }
            }

            //  Preisbereich
            if (from.HasValue || to.HasValue)
            {
                filter = new MiningFilter { Or = Enumerable.Empty<MiningFilter>() };

                filter.Or = filter.Or.Concat(new[] { new MiningFilter {
                    Target = "price",
                    Minimum = from.HasValue ? from.Value : default(object),
                    Maximum = to.HasValue ? to.Value : default(object)
                }});
            }

            return filter;
        }
        private static MiningFilter ComposeFilterSize(string question)
        {
            var filter = default(MiningFilter);

            var expression = string.Empty;
            var regularexpression = string.Empty;

            regularexpression = "größe(n)";
            expression = Regex.Match(question, $"{regularexpression} .*? (({string.Join("|", Separators)}) .*? )?")?.Value.Trim();
            expression = Regex.Replace(expression, regularexpression, string.Empty).Trim();

            //  Größenbereich
            regularexpression = $@"({string.Join("|", Separatorsrange)})";
            var sizes = Regex.Split(expression, regularexpression).Where(size => !Regex.IsMatch(size, regularexpression));
            sizes = sizes.Where(size => !string.IsNullOrEmpty(size)).Select(size => size.Trim());
            if (sizes.Any() && sizes.Count() == 2)
            {
                filter = new MiningFilter { Or = Enumerable.Empty<MiningFilter>() };

                if (sizes.Count() == 2)
                {
                    filter.Or = filter.Or.Concat(new[] { new MiningFilter {
                        Target = "clothessize",
                        Minimum = sizes.First(),
                        Maximum = sizes.Last()
                    }});
                }
            }

            //  Größenliste
            if (filter == null)
            {
                regularexpression = $@"({string.Join("|", Separatorsselection)})";
                sizes = Regex.Split(expression, regularexpression).Where(size => !Regex.IsMatch(size, regularexpression));
                sizes = sizes.Where(size => !string.IsNullOrEmpty(size)).Select(size => size.Trim());
                if (sizes.Any())
                {
                    filter = new MiningFilter { Or = Enumerable.Empty<MiningFilter>() };

                    //  Liste bestimmter Größen
                    foreach (var size in sizes)
                    {
                        filter.Or = filter.Or.Concat(new[] { new MiningFilter {
                            Target = "clothessize",
                            Value = size
                        }});
                    }
                }
            }

            return filter;
        }
    }
}