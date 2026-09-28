using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Frozen;
using System.Globalization;
using System.Net;
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

        [GeneratedRegex(
            @"^[a-zA-Z0-9_%+-]+(\.[a-zA-Z0-9_%+-]+)*@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.[a-zA-Z0-9-]{2,}$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            matchTimeoutMilliseconds: 250)]
        private static partial Regex EmailRegex();

        public EmailDomainValidatorService(
            EmailValidatorOptions? options = null,
            IMemoryCache? cache = null,
            ILookupClient? dnsClient = null,
            HttpClient? httpClient = null)
        {
            _options = options ?? new EmailValidatorOptions();
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _options.CacheSizeLimit,
                CompactionPercentage = 0.2
            });
            _dnsClient = dnsClient ?? new LookupClient(new LookupClientOptions
            {
                Timeout = _options.DnsTimeout,
                Retries = 2
            });
            _httpClient = httpClient ?? new HttpClient();
            _blocklist = DefaultBlocklist.Value;
        }

        // ── Format ──────────────────────────────────────────────────────────

        public bool IsValidFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return EmailRegex().IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
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

        /// <summary>
        /// Checks whether the email domain has resolvable MX records (sync).
        /// Note: In ASP.NET Core server applications, prefer using <see cref="HasValidMxRecordsAsync"/>
        /// to avoid blocking threadpool worker threads.
        /// </summary>
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

                SetCacheEntry(cacheKey, hasMx);
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

                SetCacheEntry(cacheKey, hasMx);
                return hasMx;
            }
            catch
            {
                return false;
            }
        }

        // ── Validate (bool) ──────────────────────────────────────────────────

        /// <summary>
        /// Runs all validation checks synchronously.
        /// Note: In server applications, prefer <see cref="ValidateEmailAsync"/> to prevent thread starvation.
        /// </summary>
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
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Blocklist URL cannot be null or empty.", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid blocklist URL format: '{url}'.", nameof(url));

            await ValidateBlocklistUrlAsync(uri, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value > _options.MaxBlocklistSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Blocklist response size ({response.Content.Headers.ContentLength.Value} bytes) exceeds maximum allowed size of {_options.MaxBlocklistSizeBytes} bytes.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var lines = new List<string>();
            long totalBytesRead = 0;
            string? line;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                totalBytesRead += (line.Length + 1);
                if (totalBytesRead > _options.MaxBlocklistSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"Blocklist download exceeded maximum allowed size of {_options.MaxBlocklistSizeBytes} bytes.");
                }

                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("//"))
                {
                    lines.Add(trimmed);
                }
            }

            _blocklist = lines.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }

        private async Task ValidateBlocklistUrlAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (_options.AllowInsecureBlocklistUrls) return;

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Insecure URL scheme '{uri.Scheme}' rejected. Blocklist updates require HTTPS for security.",
                    nameof(uri));
            }

            // SSRF protection: check if host is IP or resolves to restricted IP
            if (IPAddress.TryParse(uri.DnsSafeHost, out var ip))
            {
                if (IsPrivateOrRestrictedIp(ip))
                {
                    throw new ArgumentException(
                        $"Blocklist URL host '{uri.DnsSafeHost}' is a restricted private or link-local address.",
                        nameof(uri));
                }
            }
            else
            {
                try
                {
                    var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
                    if (addresses.Any(IsPrivateOrRestrictedIp))
                    {
                        throw new ArgumentException(
                            $"Blocklist URL host '{uri.DnsSafeHost}' resolves to a restricted private or link-local address.",
                            nameof(uri));
                    }
                }
                catch (ArgumentException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // If DNS resolution fails, HttpClient will report connection failure
                }
            }
        }

        private static bool IsPrivateOrRestrictedIp(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv4MappedToIPv6)
                    ip = ip.MapToIPv4();
                else
                    return false;
            }

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 10) return true;                                       // 10.0.0.0/8
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                if (bytes[0] == 192 && bytes[1] == 168) return true;                   // 192.168.0.0/16
                if (bytes[0] == 169 && bytes[1] == 254) return true;                   // 169.254.0.0/16 (Link Local / Cloud Metadata)
                if (bytes[0] == 127) return true;                                      // 127.0.0.0/8 (Loopback)
                if (bytes[0] == 0) return true;                                        // 0.0.0.0/8
            }

            return false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string CacheKey(string domain) => $"mx:{domain}";

        private void SetCacheEntry(string cacheKey, bool value)
        {
            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheTtl,
                Size = 1
            };
            _cache.Set(cacheKey, value, entryOptions);
        }

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
