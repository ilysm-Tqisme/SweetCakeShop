using System.Globalization;
using System.Text;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Normalizes Vietnamese text for search: removes diacritics (dấu),
    /// lowercases, and trims whitespace so that "Bánh kem" matches "banh kem".
    /// </summary>
    public interface IVietnameseNormalizerService
    {
        /// <summary>Normalize text: remove diacritics, lowercase, trim.</summary>
        string Normalize(string? input);

        /// <summary>Check if <paramref name="source"/> contains <paramref name="search"/> after normalization.</summary>
        bool FuzzyContains(string? source, string? search);

        /// <summary>
        /// Compute a relevance score (0.0–1.0) for how well <paramref name="candidate"/>
        /// matches <paramref name="query"/> after normalization.
        /// </summary>
        double Score(string? candidate, string? query);
    }

    public class VietnameseNormalizerService : IVietnameseNormalizerService
    {
        // Vietnamese-specific character replacements that Unicode decomposition
        // doesn't handle correctly (đ/Đ → d/D).
        private static readonly Dictionary<char, char> SpecialReplacements = new()
        {
            { 'đ', 'd' }, { 'Đ', 'd' }
        };

        public string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (SpecialReplacements.TryGetValue(ch, out var replacement))
                {
                    sb.Append(replacement);
                    continue;
                }
                sb.Append(ch);
            }

            // Unicode normalization FormD decomposes characters so that
            // accented chars become base char + combining mark.
            var normalized = sb.ToString().Normalize(NormalizationForm.FormD);

            var result = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                // Skip combining diacritical marks (category NonSpacingMark)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(ch);
                }
            }

            return result.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();
        }

        public bool FuzzyContains(string? source, string? search)
        {
            var normalizedSource = Normalize(source);
            var normalizedSearch = Normalize(search);

            if (string.IsNullOrEmpty(normalizedSearch))
                return true; // empty search matches everything

            if (string.IsNullOrEmpty(normalizedSource))
                return false;

            return normalizedSource.Contains(normalizedSearch, StringComparison.Ordinal);
        }

        public double Score(string? candidate, string? query)
        {
            var normalizedCandidate = Normalize(candidate);
            var normalizedQuery = Normalize(query);

            if (string.IsNullOrEmpty(normalizedQuery) || string.IsNullOrEmpty(normalizedCandidate))
                return 0.0;

            // Exact match → highest score
            if (normalizedCandidate == normalizedQuery)
                return 1.0;

            // Starts with → high score
            if (normalizedCandidate.StartsWith(normalizedQuery, StringComparison.Ordinal))
                return 0.9;

            // Contains → medium score, weighted by position
            var index = normalizedCandidate.IndexOf(normalizedQuery, StringComparison.Ordinal);
            if (index >= 0)
            {
                // Earlier position = higher score
                var positionFactor = 1.0 - ((double)index / normalizedCandidate.Length);
                return 0.5 + (positionFactor * 0.3);
            }

            // Word-level matching: check if all query words appear in candidate
            var queryWords = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var candidateWords = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (queryWords.Length > 1)
            {
                var matchedWords = queryWords.Count(qw =>
                    candidateWords.Any(cw => cw.Contains(qw, StringComparison.Ordinal)));

                if (matchedWords > 0)
                {
                    return 0.3 * ((double)matchedWords / queryWords.Length);
                }
            }

            // Edit distance fallback for typo tolerance (only for short queries)
            if (normalizedQuery.Length <= 20)
            {
                var minDistance = int.MaxValue;
                foreach (var word in candidateWords)
                {
                    var distance = LevenshteinDistance(word, normalizedQuery);
                    if (distance < minDistance)
                        minDistance = distance;
                }

                // Allow up to 2 character differences for fuzzy matching
                if (minDistance <= 2)
                {
                    return 0.2 * (1.0 - ((double)minDistance / Math.Max(normalizedQuery.Length, 1)));
                }
            }

            return 0.0;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            var m = s.Length;
            var n = t.Length;
            var d = new int[m + 1, n + 1];

            for (var i = 0; i <= m; i++) d[i, 0] = i;
            for (var j = 0; j <= n; j++) d[0, j] = j;

            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[m, n];
        }
    }
}
