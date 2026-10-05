using System;
using System.Linq;

namespace sketchDeck.Utils;

public static class SearchFilter
{
    public static bool Matches(string? name, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        name ??= string.Empty;

        var tokens = search.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);


        var positiveGroups = tokens
            .Where(x => !x.StartsWith('-'))
            .ToArray();

        var exclusions = tokens
            .Where(x => x.StartsWith('-'))
            .Select(x => x[1..])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        bool positiveMatch;

        if (positiveGroups.Length == 0)
        {

            positiveMatch = true;
        }
        else
        {
            positiveMatch = positiveGroups.Any(group =>
            {
                var andTerms = group.Split(
                    '+',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

                return andTerms.All(term =>
                    name.Contains(
                        term,
                        StringComparison.OrdinalIgnoreCase));
            });
        }

        if (!positiveMatch)
            return false;

        return exclusions.All(excluded =>
            !name.Contains(
                excluded,
                StringComparison.OrdinalIgnoreCase));
    }
}