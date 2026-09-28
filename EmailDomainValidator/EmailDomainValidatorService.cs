using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EmailDomainValidator
{
    public partial class EmailDomainValidatorService : IEmailDomainValidator
    {
        private readonly EmailValidatorOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILookupClient _dnsClient;
        private readonly HttpClient _httpClient;

        // Shared embedded blocklist loaded once lazily
        private static readonly Lazy<FrozenSet<string>> DefaultBlocklist = new(LoadEmbeddedBlocklist);

        // Instance blocklist — initialized with default embedded set, replaced atomically on update
        private volatile FrozenSet<string> _blocklist;

        private static readonly IdnMapping Idn = new();

        [GeneratedRegex(@"^[a-zA-Z0-9_%+-]+(\.[a-zA-Z0-9_%+-]+)*@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.[a-zA-Z0-9-]{2,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex EmailRegex();

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
            _blocklist = DefaultBlocklist.Value;
        }

        // ── Format ──────────────────────────────────────────────────────────

        public bool IsValidFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex().IsMatch(email);
        }

        // ── Disposable ───────────────────────────────────────────────────────

        public bool IsDisposableEmail(string email)
        {
            if (!TryGetDomain(email, out var domain))
                return false;

            var blocklist = _blocklist;
            if (blocklist.Contains(domain))
                return true;

            if (_options.BlockDisposableSubdomains)
            {
                var current = domain;
                while (true)
                {
                    int dotIndex = current.IndexOf('.');
                    if (dotIndex < 0 || dotIndex == current.Length - 1) break;
                    current = current[(dotIndex + 1)..];
                    if (blocklist.Contains(current))
                        return true;
                }
            }

            return false;
        }

        // ── MX Records ───────────────────────────────────────────────────────

        public bool HasValidMxRecords(string email)
        {
            if (!TryGetDomain(email, out var domain))
                return false;

            var cacheKey = CacheKey(domain);
            if (_cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            try
            {
                var result = _dnsClient.Query(domain, QueryType.MX);
                var hasMx = HasAcceptableMxRecords(result);

                if (!hasMx && _options.AllowAddressRecordFallback)
                {
                    var aResult = _dnsClient.Query(domain, QueryType.A);
                    hasMx = aResult.Answers.ARecords().Any();
                    if (!hasMx)
                    {
                        var aaaaResult = _dnsClient.Query(domain, QueryType.AAAA);
                        hasMx = aaaaResult.Answers.AaaaRecords().Any();
                    }
                }

                _cache.Set(cacheKey, hasMx, _options.CacheTtl);
                return hasMx;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasValidMxRecordsAsync(string email, CancellationToken cancellationToken = default)
        {
            if (!TryGetDomain(email, out var domain))
                return false;

            var cacheKey = CacheKey(domain);
            if (_cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            try
            {
                var result = await _dnsClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
                var hasMx = HasAcceptableMxRecords(result);

                if (!hasMx && _options.AllowAddressRecordFallback)
                {
                    var aResult = await _dnsClient.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
                    hasMx = aResult.Answers.ARecords().Any();
                    if (!hasMx)
                    {
                        var aaaaResult = await _dnsClient.QueryAsync(domain, QueryType.AAAA, cancellationToken: cancellationToken);
                        hasMx = aaaaResult.Answers.AaaaRecords().Any();
                    }
                }

                _cache.Set(cacheKey, hasMx, _options.CacheTtl);
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

        public async Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (!IsValidFormat(email)) return false;
            if (IsDisposableEmail(email)) return false;
            if (!await HasValidMxRecordsAsync(email, cancellationToken)) return false;
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

        public async Task<ValidationResult> ValidateEmailWithResultAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidFormat(email))
                return ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
            if (IsDisposableEmail(email))
                return ValidationResult.Fail(ValidationFailureReason.DisposableDomain);
            if (!await HasValidMxRecordsAsync(email, cancellationToken))
                return ValidationResult.Fail(ValidationFailureReason.NoMxRecords);
            return ValidationResult.Success();
        }

        // ── Blocklist update ─────────────────────────────────────────────────

        public async Task UpdateBlocklistAsync(string url, CancellationToken cancellationToken = default)
        {
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            var lines = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("//"));
            _blocklist = lines.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string CacheKey(string domain) => $"mx:{domain}";

        public static bool TryGetDomain(string email, out string domain)
        {
            domain = string.Empty;
            if (string.IsNullOrWhiteSpace(email)) return false;
            int atIndex = email.LastIndexOf('@');
            if (atIndex <= 0 || atIndex >= email.Length - 1) return false;

            var rawDomain = email[(atIndex + 1)..].Trim().TrimEnd('.');
            if (rawDomain.Length == 0) return false;

            try
            {
                domain = Idn.GetAscii(rawDomain).ToLowerInvariant();
                return domain.Length > 0;
            }
            catch
            {
                domain = rawDomain.ToLowerInvariant();
                return domain.Length > 0;
            }
        }

        private bool HasAcceptableMxRecords(IDnsQueryResponse result)
        {
            var records = result.Answers.MxRecords();
            if (_options.RejectNullMx)
            {
                return records.Any(mx =>
                {
                    var exchange = mx.Exchange?.Value?.Trim();
                    return !string.IsNullOrEmpty(exchange) && exchange != ".";
                });
            }
            return records.Any();
        }

        private static FrozenSet<string> LoadEmbeddedBlocklist()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "EmailDomainValidator.disposable_email_blocklist.conf";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);

            var lines = reader.ReadToEnd()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("//"));

            return lines.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
