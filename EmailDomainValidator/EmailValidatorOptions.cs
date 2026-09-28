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
    }
}
