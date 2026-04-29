using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EmailDomainValidator
{
    public class EmailDomainValidatorService : IEmailDomainValidator
    {
        private readonly EmailValidatorOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILookupClient _dnsClient;
        private readonly HttpClient _httpClient;

        // Shared blocklist — swapped atomically on update
        private volatile HashSet<string> _blocklist;

        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        public EmailDomainValidatorService(
            EmailValidatorOptions? options = null,
            IMemoryCache? cache = null,
            ILookupClient? dnsClient = null,
            HttpClient? httpClient = null)
        {
            _options = options ?? new EmailValidatorOptions();
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
            _dnsClient = dnsClient ?? new LookupClient();
            _httpClient = httpClient ?? new HttpClient();
            _blocklist = LoadEmbeddedBlocklist();
        }

        // ── Format ──────────────────────────────────────────────────────────

        public bool IsValidFormat(string email) => EmailRegex.IsMatch(email);

        // ── Disposable ───────────────────────────────────────────────────────

        public bool IsDisposableEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            return _blocklist.Contains(parts[1]);
        }

        // ── MX Records ───────────────────────────────────────────────────────

        public bool HasValidMxRecords(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            var domain = parts[1];

            if (_cache.TryGetValue(CacheKey(domain), out bool cached))
                return cached;

            try
            {
                var result = _dnsClient.Query(domain, QueryType.MX);
                var hasMx = result.Answers.MxRecords().Any();
                _cache.Set(CacheKey(domain), hasMx, _options.CacheTtl);
                return hasMx;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasValidMxRecordsAsync(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                return false;
            var domain = parts[1];

            if (_cache.TryGetValue(CacheKey(domain), out bool cached))
                return cached;

            try
            {
                var result = await _dnsClient.QueryAsync(domain, QueryType.MX);
                var hasMx = result.Answers.MxRecords().Any();
                _cache.Set(CacheKey(domain), hasMx, _options.CacheTtl);
                return hasMx;
            }
            catch
            {
                return false;
            }
        }

        // ── Validate (bool) ──────────────────────────────────────────────────

        public bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (!IsValidFormat(email)) return false;
            if (IsDisposableEmail(email)) return false;
            if (!HasValidMxRecords(email)) return false;
            return true;
        }

        public async Task<bool> ValidateEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (!IsValidFormat(email)) return false;
            if (IsDisposableEmail(email)) return false;
            if (!await HasValidMxRecordsAsync(email)) return false;
            return true;
        }

        // ── Validate (ValidationResult) ──────────────────────────────────────

        public ValidationResult ValidateEmailWithResult(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidFormat(email))
                return ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
            if (IsDisposableEmail(email))
                return ValidationResult.Fail(ValidationFailureReason.DisposableDomain);
            if (!HasValidMxRecords(email))
                return ValidationResult.Fail(ValidationFailureReason.NoMxRecords);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult> ValidateEmailWithResultAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidFormat(email))
                return ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
            if (IsDisposableEmail(email))
                return ValidationResult.Fail(ValidationFailureReason.DisposableDomain);
            if (!await HasValidMxRecordsAsync(email))
                return ValidationResult.Fail(ValidationFailureReason.NoMxRecords);
            return ValidationResult.Success();
        }

        // ── Blocklist update ─────────────────────────────────────────────────

        public async Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default)
        {
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            var lines = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));
            _blocklist = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string CacheKey(string domain) => $"mx:{domain}";

        private static HashSet<string> LoadEmbeddedBlocklist()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "EmailDomainValidator.disposable_email_blocklist.conf";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);

            var lines = reader.ReadToEnd()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));

            return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        }
    }
}
