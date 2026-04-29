namespace EmailDomainValidator
{
    public interface IEmailDomainValidator
    {
        /// <summary>Checks whether the email address has a valid format.</summary>
        bool IsValidFormat(string email);

        /// <summary>Checks whether the email domain is on the disposable-email blocklist.</summary>
        bool IsDisposableEmail(string email);

        /// <summary>Checks whether the email domain has resolvable MX records (sync).</summary>
        bool HasValidMxRecords(string email);

        /// <summary>Checks whether the email domain has resolvable MX records (async).</summary>
        Task<bool> HasValidMxRecordsAsync(string email);

        /// <summary>Runs all validation checks synchronously.</summary>
        bool ValidateEmail(string email);

        /// <summary>Runs all validation checks asynchronously.</summary>
        Task<bool> ValidateEmailAsync(string email);

        /// <summary>Runs all validation checks synchronously and returns a detailed result.</summary>
        ValidationResult ValidateEmailWithResult(string email);

        /// <summary>Runs all validation checks asynchronously and returns a detailed result.</summary>
        Task<ValidationResult> ValidateEmailWithResultAsync(string email);

        /// <summary>Fetches a fresh blocklist from <paramref name="url"/> and replaces the in-memory set.</summary>
        Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default);
    }
}
