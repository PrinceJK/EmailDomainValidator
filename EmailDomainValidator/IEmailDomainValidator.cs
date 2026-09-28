namespace EmailDomainValidator
{
    public interface IEmailDomainValidator
    {
        /// <summary>Checks whether the email address has a valid format.</summary>
        bool IsValidFormat(string email);

        /// <summary>Checks whether the email domain is on the disposable-email blocklist.</summary>
        bool IsDisposableEmail(string email);

        /// <summary>Checks whether the email domain belongs to a free consumer webmail provider (e.g. gmail.com, yahoo.com).</summary>
        bool IsFreeWebmail(string email);

        /// <summary>Checks whether the email address has a generic/role-based mailbox prefix (e.g. admin@, support@, info@).</summary>
        bool IsRoleBasedEmail(string email);

        /// <summary>Attempts to find a suggested domain correction for common typos (e.g. user@gamil.com -> user@gmail.com).</summary>
        string? SuggestDomainCorrection(string email);

        /// <summary>Checks whether the email domain has resolvable MX records (sync).</summary>
        bool HasValidMxRecords(string email);

        /// <summary>Checks whether the email domain has resolvable MX records (async).</summary>
        Task<bool> HasValidMxRecordsAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>Runs all validation checks synchronously.</summary>
        bool ValidateEmail(string email);

        /// <summary>Runs all validation checks asynchronously.</summary>
        Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>Runs all validation checks synchronously and returns a detailed result.</summary>
        ValidationResult ValidateEmailWithResult(string email);

        /// <summary>Runs all validation checks asynchronously and returns a detailed result.</summary>
        Task<ValidationResult> ValidateEmailWithResultAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>Validates a batch of email addresses concurrently with streaming results.</summary>
        IAsyncEnumerable<ValidationResult> ValidateBatchAsync(
            IEnumerable<string> emails,
            int maxConcurrency = 10,
            CancellationToken cancellationToken = default);

        /// <summary>Fetches a fresh blocklist from <paramref name="url"/> and replaces the in-memory set.</summary>
        Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default);
    }
}
