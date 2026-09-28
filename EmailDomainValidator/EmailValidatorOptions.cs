namespace EmailDomainValidator
{
    public class EmailValidatorOptions
    {
        /// <summary>
        /// How long DNS MX lookup results are cached. Defaults to 1 hour.
        /// </summary>
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Optional URL to fetch an up-to-date disposable email blocklist from.
        /// When null the embedded blocklist is used exclusively.
        /// </summary>
        public string? BlocklistUpdateUrl { get; set; }

        /// <summary>
        /// When true, falls back to checking A/AAAA records if no MX records are found (RFC 5321 §5.1).
        /// Defaults to false.
        /// </summary>
        public bool AllowAddressRecordFallback { get; set; } = false;

        /// <summary>
        /// When true, subdomains of blocked disposable domains are also blocked (e.g., sub.mailinator.com).
        /// Defaults to true.
        /// </summary>
        public bool BlockDisposableSubdomains { get; set; } = true;

        /// <summary>
        /// When true, rejects RFC 7505 Null MX records (preference 0 and exchange ".") used to signal that a domain rejects all email.
        /// Defaults to false for backwards compatibility.
        /// </summary>
        public bool RejectNullMx { get; set; } = false;

        /// <summary>
        /// Maximum number of DNS lookup entries to hold in memory cache to prevent unbounded memory growth (Cache DoS).
        /// Defaults to 50,000 entries.
        /// </summary>
        public int CacheSizeLimit { get; set; } = 50_000;

        /// <summary>
        /// Timeout for DNS queries. Defaults to 3 seconds to mitigate threadpool starvation.
        /// </summary>
        public TimeSpan DnsTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Maximum response size in bytes allowed when updating the blocklist via HTTP.
        /// Defaults to 10 MB to prevent memory bombs.
        /// </summary>
        public long MaxBlocklistSizeBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// When false (default), enforces HTTPS and blocks loopback, private, and cloud metadata IP addresses
        /// when downloading blocklists (SSRF protection). Set to true only in internal test environments.
        /// </summary>
        public bool AllowInsecureBlocklistUrls { get; set; } = false;

        /// <summary>
        /// When true (default), attempts to detect typos for popular domains (e.g. gamil.com -> gmail.com).
        /// Suggested correction is available on <see cref="ValidationResult.SuggestedEmail"/>.
        /// </summary>
        public bool EnableTypoSuggestions { get; set; } = true;

        /// <summary>
        /// Maximum Damerau-Levenshtein edit distance to trigger a typo suggestion. Defaults to 2.
        /// </summary>
        public int MaxTypoDistance { get; set; } = 2;

        /// <summary>
        /// When false, rejects free consumer webmail providers (e.g. gmail.com, yahoo.com, outlook.com).
        /// Defaults to true. Useful for B2B SaaS applications requiring business/work email addresses.
        /// </summary>
        public bool AllowFreeWebmail { get; set; } = true;

        /// <summary>
        /// When false, rejects generic/role-based mailboxes (e.g. admin@, support@, info@, sales@, billing@).
        /// Defaults to true.
        /// </summary>
        public bool AllowRoleBasedEmails { get; set; } = true;

        /// <summary>
        /// Optional custom set of blocked domains (e.g. competitor domains or known abuse sources).
        /// Checked case-insensitively.
        /// </summary>
        public ISet<string>? BlockedDomains { get; set; }

        /// <summary>
        /// Optional whitelist of permitted domains. If populated, only emails belonging to these domains will pass.
        /// Checked case-insensitively.
        /// </summary>
        public ISet<string>? AllowedDomains { get; set; }

        /// <summary>
        /// Optional custom set of blocked Top-Level Domains (e.g. [".xyz", ".top", ".buzz"]).
        /// Checked case-insensitively.
        /// </summary>
        public ISet<string>? BlockedTlds { get; set; }
    }
}
