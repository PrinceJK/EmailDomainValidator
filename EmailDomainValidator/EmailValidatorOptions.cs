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
    }
}
