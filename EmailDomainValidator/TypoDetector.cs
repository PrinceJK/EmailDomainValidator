namespace EmailDomainValidator
{
    /// <summary>
    /// Utility for detecting common domain typos using Damerau-Levenshtein distance against popular email domains.
    /// </summary>
    public static class TypoDetector
    {
        public static readonly string[] PopularDomains =
        [
            "gmail.com", "googlemail.com", "yahoo.com", "ymail.com", "rocketmail.com",
            "hotmail.com", "outlook.com", "live.com", "msn.com",
            "icloud.com", "me.com", "mac.com",
            "aol.com", "aim.com",
            "protonmail.com", "proton.me",
            "zoho.com", "zohomail.com",
            "mail.com", "email.com", "usa.com",
            "gmx.com", "gmx.net", "gmx.de",
            "yandex.com", "yandex.ru", "ya.ru",
            "fastmail.com",
            "comcast.net", "sbcglobal.net", "att.net", "verizon.net", "cox.net", "bellsouth.net",
            "charter.net", "earthlink.net",
            "t-online.de", "web.de", "freenet.de",
            "orange.fr", "free.fr", "sfr.fr", "laposte.net", "wanadoo.fr",
            "libero.it", "virgilio.it", "alice.it",
            "uol.com.br", "bol.com.br", "terra.com.br",
            "mail.ru", "inbox.ru", "list.ru", "bk.ru",
            "qq.com", "163.com", "126.com", "sina.com",
            "rediffmail.com"
        ];

        /// <summary>
        /// Attempts to find a close match for <paramref name="domain"/> among popular email providers.
        /// Returns null if <paramref name="domain"/> is already an exact match or no close match is found within <paramref name="maxDistance"/>.
        /// </summary>
        public static string? SuggestDomain(string domain, int maxDistance = 2)
        {
            if (string.IsNullOrWhiteSpace(domain)) return null;

            var cleanDomain = domain.Trim().TrimEnd('.').ToLowerInvariant();

            string? bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var popular in PopularDomains)
            {
                if (string.Equals(cleanDomain, popular, StringComparison.OrdinalIgnoreCase))
                    return null; // Already an exact match, no typo

                int dist = DamerauLevenshteinDistance(cleanDomain, popular);
                if (dist <= maxDistance && dist < bestDistance)
                {
                    bestDistance = dist;
                    bestMatch = popular;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein distance (insertions, deletions, substitutions, transpositions).
        /// </summary>
        public static int DamerauLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target)) return source.Length;

            int lenSource = source.Length;
            int lenTarget = target.Length;

            if (Math.Abs(lenSource - lenTarget) > 3) return int.MaxValue;

            var d = new int[lenSource + 1, lenTarget + 1];

            for (int i = 0; i <= lenSource; i++) d[i, 0] = i;
            for (int j = 0; j <= lenTarget; j++) d[0, j] = j;

            for (int i = 1; i <= lenSource; i++)
            {
                for (int j = 1; j <= lenTarget; j++)
                {
                    int cost = char.ToLowerInvariant(source[i - 1]) == char.ToLowerInvariant(target[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);

                    if (i > 1 && j > 1 &&
                        char.ToLowerInvariant(source[i - 1]) == char.ToLowerInvariant(target[j - 2]) &&
                        char.ToLowerInvariant(source[i - 2]) == char.ToLowerInvariant(target[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[lenSource, lenTarget];
        }
    }
}
