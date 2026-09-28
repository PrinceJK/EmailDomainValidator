namespace EmailDomainValidator
{
    /// <summary>
    /// Static facade for email domain validation.
    /// Delegates to a shared default instance of <see cref="EmailDomainValidatorService"/>.
    /// </summary>
    public static class EmailValidator
    {
        private static readonly Lazy<EmailDomainValidatorService> DefaultService =
            new(() => new EmailDomainValidatorService());

        /// <summary>Checks whether the email address has a valid format.</summary>
        public static bool IsValidFormat(string email) =>
            DefaultService.Value.IsValidFormat(email);

        /// <summary>Checks whether the email domain is on the disposable-email blocklist.</summary>
        public static bool IsDisposableEmail(string email) =>
            DefaultService.Value.IsDisposableEmail(email);

        /// <summary>Checks whether the email domain belongs to a free consumer webmail provider (e.g. gmail.com, yahoo.com).</summary>
        public static bool IsFreeWebmail(string email) =>
            DefaultService.Value.IsFreeWebmail(email);

        /// <summary>Checks whether the email address has a generic/role-based mailbox prefix (e.g. admin@, support@, info@).</summary>
        public static bool IsRoleBasedEmail(string email) =>
            DefaultService.Value.IsRoleBasedEmail(email);

        /// <summary>Attempts to find a suggested domain correction for common typos (e.g. user@gamil.com -> user@gmail.com).</summary>
        public static string? SuggestDomainCorrection(string email) =>
            DefaultService.Value.SuggestDomainCorrection(email);

        /// <summary>Checks whether the email domain has resolvable MX records (sync).</summary>
        public static bool HasValidMxRecords(string email) =>
            DefaultService.Value.HasValidMxRecords(email);

        /// <summary>Checks whether the email domain has resolvable MX records (async).</summary>
        public static Task<bool> HasValidMxRecordsAsync(string email, CancellationToken cancellationToken = default) =>
            DefaultService.Value.HasValidMxRecordsAsync(email, cancellationToken);

        /// <summary>Runs all validation checks synchronously.</summary>
        public static bool ValidateEmail(string email) =>
            DefaultService.Value.ValidateEmail(email);

        /// <summary>Runs all validation checks asynchronously.</summary>
        public static Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default) =>
            DefaultService.Value.ValidateEmailAsync(email, cancellationToken);

        /// <summary>Runs all validation checks synchronously and returns a detailed result.</summary>
        public static ValidationResult ValidateEmailWithResult(string email) =>
            DefaultService.Value.ValidateEmailWithResult(email);

        /// <summary>Runs all validation checks asynchronously and returns a detailed result.</summary>
        public static Task<ValidationResult> ValidateEmailWithResultAsync(string email, CancellationToken cancellationToken = default) =>
            DefaultService.Value.ValidateEmailWithResultAsync(email, cancellationToken);

        /// <summary>Validates a batch of email addresses concurrently with streaming results.</summary>
        public static IAsyncEnumerable<ValidationResult> ValidateBatchAsync(
            IEnumerable<string> emails,
            int maxConcurrency = 10,
            CancellationToken cancellationToken = default) =>
            DefaultService.Value.ValidateBatchAsync(emails, maxConcurrency, cancellationToken);

        /// <summary>
        /// Fetches a fresh blocklist from <paramref name="url"/> and replaces the in-memory set.
        /// </summary>
        public static Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default) =>
            DefaultService.Value.UpdateBlocklistAsync(url, cancellationToken);
    }
}
