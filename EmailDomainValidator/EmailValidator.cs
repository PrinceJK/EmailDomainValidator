using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EmailDomainValidator
{
    public class EmailValidator
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly LookupClient DnsLookup = new LookupClient();
        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(1);

        private static string CacheKey(string domain) => $"mx:{domain}";

        public static bool HasValidMxRecords(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            var domain = parts[1];
            if (Cache.TryGetValue(CacheKey(domain), out bool hasMxRecords))
                return hasMxRecords;

            try
            {
                var result = DnsLookup.Query(domain, QueryType.MX);
                hasMxRecords = result.Answers.MxRecords().Any();
                Cache.Set(CacheKey(domain), hasMxRecords, DefaultCacheTtl);
                return hasMxRecords;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> HasValidMxRecordsAsync(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            var domain = parts[1];
            if (Cache.TryGetValue(CacheKey(domain), out bool hasMxRecords))
                return hasMxRecords;

            try
            {
                var result = await DnsLookup.QueryAsync(domain, QueryType.MX);
                hasMxRecords = result.Answers.MxRecords().Any();
                Cache.Set(CacheKey(domain), hasMxRecords, DefaultCacheTtl);
                return hasMxRecords;
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (!IsValidFormat(email))
                return false;

            if (IsDisposableEmail(email))
                return false;

            if (!HasValidMxRecords(email))
                return false;

            return true;
        }

        public static async Task<bool> ValidateEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (!IsValidFormat(email))
                return false;

            if (IsDisposableEmail(email))
                return false;

            if (!await HasValidMxRecordsAsync(email))
                return false;

            return true;
        }

        public static ValidationResult ValidateEmailWithResult(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidFormat(email))
                return ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
            if (IsDisposableEmail(email))
                return ValidationResult.Fail(ValidationFailureReason.DisposableDomain);
            if (!HasValidMxRecords(email))
                return ValidationResult.Fail(ValidationFailureReason.NoMxRecords);
            return ValidationResult.Success();
        }

        public static async Task<ValidationResult> ValidateEmailWithResultAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidFormat(email))
                return ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
            if (IsDisposableEmail(email))
                return ValidationResult.Fail(ValidationFailureReason.DisposableDomain);
            if (!await HasValidMxRecordsAsync(email))
                return ValidationResult.Fail(ValidationFailureReason.NoMxRecords);
            return ValidationResult.Success();
        }

        private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

        public static bool IsValidFormat(string email)
        {
            return EmailRegex.IsMatch(email);
        }

        public static bool IsDisposableEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            return ActiveBlocklist.Contains(parts[1]);
        }

        private static readonly Lazy<HashSet<string>> _emailBlockList = new Lazy<HashSet<string>>(() =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "EmailDomainValidator.disposable_email_blocklist.conf";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);

            var lines = reader.ReadToEnd()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));

            return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        });

        // Swapped atomically when UpdateBlocklistAsync is called
        private static volatile HashSet<string>? _updatedBlocklist;

        private static HashSet<string> ActiveBlocklist =>
            _updatedBlocklist ?? _emailBlockList.Value;

        /// <summary>
        /// Fetches a fresh blocklist from <paramref name="url"/> and replaces the in-memory set
        /// used by the static helpers.
        /// </summary>
        public static async Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default)
        {
            using var http = new System.Net.Http.HttpClient();
            var content = await http.GetStringAsync(url, cancellationToken);
            var lines = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));
            _updatedBlocklist = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        }
    }
}
