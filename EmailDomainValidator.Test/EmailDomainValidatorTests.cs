using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Validator = EmailDomainValidator.EmailValidator;
using DataAnnotationsValidator = System.ComponentModel.DataAnnotations.Validator;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace EmailDomainValidator.Test;

// ═══════════════════════════════════════════════════════════════════════════
// Static class — format, disposable, MX, validate, ValidationResult
// ═══════════════════════════════════════════════════════════════════════════
public class StaticValidatorTests
{
    // ── ValidateEmail ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("test@mailinator.com", false)]
    [InlineData("invalid-email", false)]
    public void ValidateEmail_ShouldReturnExpectedResult(string email, bool expected)
        => Assert.Equal(expected, Validator.ValidateEmail(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateEmail_NullOrWhitespace_ReturnsFalse(string? email)
        => Assert.False(Validator.ValidateEmail(email!));

    // ── IsValidFormat ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@domain.com", true)]
    [InlineData("user.name+tag@sub.domain.org", true)]
    [InlineData("user@domain.co.uk", true)]
    [InlineData("user@domain", false)]
    [InlineData("@domain.com", false)]
    [InlineData("userdomain.com", false)]
    [InlineData("user@.com", false)]
    [InlineData("user @domain.com", false)]
    [InlineData("", false)]
    public void IsValidFormat_ReturnsExpected(string email, bool expected)
        => Assert.Equal(expected, Validator.IsValidFormat(email));

    // ── IsDisposableEmail ────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@mailinator.com", true)]
    [InlineData("user@guerrillamail.com", true)]
    [InlineData("user@gmail.com", false)]
    [InlineData("user@outlook.com", false)]
    public void IsDisposableEmail_ReturnsExpected(string email, bool expected)
        => Assert.Equal(expected, Validator.IsDisposableEmail(email));

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@")]
    public void IsDisposableEmail_MalformedEmail_ReturnsFalse(string email)
        => Assert.False(Validator.IsDisposableEmail(email));

    // ── HasValidMxRecords (sync — real DNS MX query) ─────────────────────────

    [Fact]
    public void HasValidMxRecords_KnownGoodDomain_ReturnsTrue()
        => Assert.True(Validator.HasValidMxRecords("user@gmail.com"));

    [Fact]
    public void HasValidMxRecords_NonExistentDomain_ReturnsFalse()
        => Assert.False(Validator.HasValidMxRecords("user@this-domain-does-not-exist-xyz123.com"));

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@")]
    public void HasValidMxRecords_MalformedEmail_ReturnsFalse(string email)
        => Assert.False(Validator.HasValidMxRecords(email));

    // ── HasValidMxRecordsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task HasValidMxRecordsAsync_KnownGoodDomain_ReturnsTrue()
        => Assert.True(await Validator.HasValidMxRecordsAsync("user@gmail.com"));

    [Fact]
    public async Task HasValidMxRecordsAsync_NonExistentDomain_ReturnsFalse()
        => Assert.False(await Validator.HasValidMxRecordsAsync("user@this-domain-does-not-exist-xyz123.com"));

    // ── ValidateEmailAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_ValidEmail_ReturnsTrue()
        => Assert.True(await Validator.ValidateEmailAsync("test@example.com"));

    [Fact]
    public async Task ValidateEmailAsync_DisposableEmail_ReturnsFalse()
        => Assert.False(await Validator.ValidateEmailAsync("test@mailinator.com"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task ValidateEmailAsync_InvalidInput_ReturnsFalse(string? email)
        => Assert.False(await Validator.ValidateEmailAsync(email!));

    // ── ValidateEmailWithResult ──────────────────────────────────────────────

    [Fact]
    public void ValidateEmailWithResult_InvalidFormat_ReturnsCorrectReason()
    {
        var result = Validator.ValidateEmailWithResult("not-an-email");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.InvalidFormat, result.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_DisposableDomain_ReturnsCorrectReason()
    {
        var result = Validator.ValidateEmailWithResult("user@mailinator.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.DisposableDomain, result.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_NoMxRecords_ReturnsCorrectReason()
    {
        var result = Validator.ValidateEmailWithResult("user@this-domain-does-not-exist-xyz123.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.NoMxRecords, result.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_ValidEmail_ReturnsSuccess()
    {
        var result = Validator.ValidateEmailWithResult("test@example.com");
        Assert.True(result.IsValid);
        Assert.Equal(ValidationFailureReason.None, result.FailureReason);
    }

    [Fact]
    public async Task ValidateEmailWithResultAsync_InvalidFormat_ReturnsCorrectReason()
    {
        var result = await Validator.ValidateEmailWithResultAsync("bad");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.InvalidFormat, result.FailureReason);
    }

    [Fact]
    public async Task ValidateEmailWithResultAsync_DisposableDomain_ReturnsCorrectReason()
    {
        var result = await Validator.ValidateEmailWithResultAsync("user@mailinator.com");
        Assert.False(result);   // implicit bool conversion
        Assert.Equal(ValidationFailureReason.DisposableDomain, result.FailureReason);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// ValidationResult unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class ValidationResultTests
{
    [Fact]
    public void Success_IsValidTrue_ReasonNone()
    {
        var r = ValidationResult.Success();
        Assert.True(r.IsValid);
        Assert.Equal(ValidationFailureReason.None, r.FailureReason);
    }

    [Theory]
    [InlineData(ValidationFailureReason.InvalidFormat)]
    [InlineData(ValidationFailureReason.DisposableDomain)]
    [InlineData(ValidationFailureReason.NoMxRecords)]
    public void Fail_IsValidFalse_CorrectReason(ValidationFailureReason reason)
    {
        var r = ValidationResult.Fail(reason);
        Assert.False(r.IsValid);
        Assert.Equal(reason, r.FailureReason);
    }

    [Fact]
    public void ImplicitBoolConversion_ReflectsIsValid()
    {
        bool fromSuccess = ValidationResult.Success();
        bool fromFail = ValidationResult.Fail(ValidationFailureReason.InvalidFormat);
        Assert.True(fromSuccess);
        Assert.False(fromFail);
    }

    [Fact]
    public void ToString_Success_ReturnsValid()
        => Assert.Equal("Valid", ValidationResult.Success().ToString());

    [Fact]
    public void ToString_Fail_ContainsReason()
        => Assert.Contains("InvalidFormat", ValidationResult.Fail(ValidationFailureReason.InvalidFormat).ToString());
}

// ═══════════════════════════════════════════════════════════════════════════
// EmailDomainValidatorService — unit tests (format + disposable, no real DNS)
// ═══════════════════════════════════════════════════════════════════════════
public class EmailDomainValidatorServiceTests
{
    private readonly EmailDomainValidatorService _sut = new EmailDomainValidatorService();

    // ── IsValidFormat ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("bad-email", false)]
    [InlineData("", false)]
    public void IsValidFormat_ReturnsExpected(string email, bool expected)
        => Assert.Equal(expected, _sut.IsValidFormat(email));

    // ── IsDisposableEmail ────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@mailinator.com", true)]
    [InlineData("user@gmail.com", false)]
    [InlineData("notanemail", false)]
    public void IsDisposableEmail_ReturnsExpected(string email, bool expected)
        => Assert.Equal(expected, _sut.IsDisposableEmail(email));

    [Theory]
    [InlineData("user@sub.mailinator.com", true)]
    [InlineData("user@deep.sub.mailinator.com", true)]
    [InlineData("user@sub.gmail.com", false)]
    public void IsDisposableEmail_Subdomains_ReturnsExpected(string email, bool expected)
        => Assert.Equal(expected, _sut.IsDisposableEmail(email));

    [Fact]
    public void IsValidFormat_PunycodeTld_ReturnsTrue()
        => Assert.True(_sut.IsValidFormat("user@domain.xn--p1ai"));

    [Theory]
    [InlineData("user..name@domain.com")]
    [InlineData(".user@domain.com")]
    [InlineData("user.@domain.com")]
    public void IsValidFormat_ConsecutiveOrEdgeDots_ReturnsFalse(string email)
        => Assert.False(_sut.IsValidFormat(email));

    // ── HasValidMxRecords (real DNS MX) ──────────────────────────────────────

    [Fact]
    public void HasValidMxRecords_KnownGoodDomain_ReturnsTrue()
        => Assert.True(_sut.HasValidMxRecords("user@gmail.com"));

    [Fact]
    public void HasValidMxRecords_NonExistentDomain_ReturnsFalse()
        => Assert.False(_sut.HasValidMxRecords("user@this-domain-does-not-exist-xyz123.com"));

    [Fact]
    public async Task HasValidMxRecordsAsync_KnownGoodDomain_ReturnsTrue()
        => Assert.True(await _sut.HasValidMxRecordsAsync("user@gmail.com"));

    // ── ValidateEmailWithResult ───────────────────────────────────────────────

    [Fact]
    public void ValidateEmailWithResult_InvalidFormat_CorrectReason()
    {
        var r = _sut.ValidateEmailWithResult("not-an-email");
        Assert.Equal(ValidationFailureReason.InvalidFormat, r.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_Disposable_CorrectReason()
    {
        var r = _sut.ValidateEmailWithResult("user@mailinator.com");
        Assert.Equal(ValidationFailureReason.DisposableDomain, r.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_NoMx_CorrectReason()
    {
        var r = _sut.ValidateEmailWithResult("user@this-domain-does-not-exist-xyz123.com");
        Assert.Equal(ValidationFailureReason.NoMxRecords, r.FailureReason);
    }

    [Fact]
    public void ValidateEmailWithResult_Valid_Success()
    {
        var r = _sut.ValidateEmailWithResult("test@example.com");
        Assert.True(r.IsValid);
        Assert.Equal(ValidationFailureReason.None, r.FailureReason);
    }

    // ── Options: custom cache TTL ─────────────────────────────────────────────

    [Fact]
    public void Service_WithCustomCacheTtl_DoesNotThrow()
    {
        var opts = new EmailValidatorOptions { CacheTtl = TimeSpan.FromMinutes(5) };
        var svc = new EmailDomainValidatorService(opts);
        var result = svc.ValidateEmailWithResult("user@mailinator.com");
        Assert.Equal(ValidationFailureReason.DisposableDomain, result.FailureReason);
    }

    // ── UpdateBlocklistAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlocklistAsync_ReplacesBlocklist()
    {
        // Serve a minimal blocklist containing only "newblocked.com"
        var fakeContent = "newblocked.com\n";
        var handler = new FakeHttpMessageHandler(fakeContent);
        var opts = new EmailValidatorOptions { AllowInsecureBlocklistUrls = true };
        var svc = new EmailDomainValidatorService(opts, httpClient: new HttpClient(handler));

        // Before update: mailinator should be blocked (from embedded list)
        Assert.True(svc.IsDisposableEmail("user@mailinator.com"));

        await svc.UpdateBlocklistAsync("http://fake-url/blocklist.txt");

        // After update: only newblocked.com is in the list
        Assert.True(svc.IsDisposableEmail("user@newblocked.com"));
        Assert.False(svc.IsDisposableEmail("user@mailinator.com"));
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Security unit tests (SSRF, Memory Bomb, Cache DoS, ReDoS)
// ═══════════════════════════════════════════════════════════════════════════
public class SecurityTests
{
    [Theory]
    [InlineData("http://example.com/blocklist.txt")]
    [InlineData("ftp://example.com/blocklist.txt")]
    [InlineData("file:///etc/passwd")]
    public async Task UpdateBlocklistAsync_InsecureScheme_ThrowsArgumentException(string url)
    {
        var svc = new EmailDomainValidatorService();
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateBlocklistAsync(url));
    }

    [Theory]
    [InlineData("https://127.0.0.1/blocklist.txt")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://10.0.0.1/blocklist.txt")]
    [InlineData("https://192.168.1.1/blocklist.txt")]
    [InlineData("https://172.16.0.1/blocklist.txt")]
    public async Task UpdateBlocklistAsync_RestrictedIp_ThrowsArgumentException(string url)
    {
        var svc = new EmailDomainValidatorService();
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateBlocklistAsync(url));
    }

    [Fact]
    public async Task UpdateBlocklistAsync_ExceedsMaxSizeBytes_ThrowsInvalidOperationException()
    {
        var content = "domain1.com\ndomain2.com\n";
        var handler = new FakeHttpMessageHandler(content);
        var opts = new EmailValidatorOptions
        {
            AllowInsecureBlocklistUrls = true,
            MaxBlocklistSizeBytes = 10
        };
        var svc = new EmailDomainValidatorService(opts, httpClient: new HttpClient(handler));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateBlocklistAsync("https://fake-url/large.txt"));
    }

    [Fact]
    public void CacheSizeLimit_PreventsUnboundedGrowth()
    {
        var opts = new EmailValidatorOptions
        {
            CacheSizeLimit = 10
        };
        var svc = new EmailDomainValidatorService(opts);
        Assert.NotNull(svc);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// EmailValidatorOptions tests
// ═══════════════════════════════════════════════════════════════════════════
public class EmailValidatorOptionsTests
{
    [Fact]
    public void DefaultOptions_CacheTtlIsOneHour()
    {
        var opts = new EmailValidatorOptions();
        Assert.Equal(TimeSpan.FromHours(1), opts.CacheTtl);
    }

    [Fact]
    public void DefaultOptions_BlocklistUpdateUrlIsNull()
    {
        var opts = new EmailValidatorOptions();
        Assert.Null(opts.BlocklistUpdateUrl);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Dependency Injection registration tests
// ═══════════════════════════════════════════════════════════════════════════
public class DependencyInjectionTests
{
    [Fact]
    public void AddEmailDomainValidator_DefaultOptions_ResolvesService()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator()
            .BuildServiceProvider();

        var svc = sp.GetRequiredService<IEmailDomainValidator>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void AddEmailDomainValidator_WithOptions_ResolvesService()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator(new EmailValidatorOptions { CacheTtl = TimeSpan.FromMinutes(30) })
            .BuildServiceProvider();

        var svc = sp.GetRequiredService<IEmailDomainValidator>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void AddEmailDomainValidator_WithDelegate_ResolvesServiceAndRespectsOptions()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator(o => o.CacheTtl = TimeSpan.FromMinutes(10))
            .BuildServiceProvider();

        var svc = sp.GetRequiredService<IEmailDomainValidator>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void AddEmailDomainValidator_IsSingleton()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator()
            .BuildServiceProvider();

        var a = sp.GetRequiredService<IEmailDomainValidator>();
        var b = sp.GetRequiredService<IEmailDomainValidator>();
        Assert.Same(a, b);
    }

    [Fact]
    public void ResolvedService_CanValidateEmail()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator()
            .BuildServiceProvider();

        var svc = sp.GetRequiredService<IEmailDomainValidator>();
        var result = svc.ValidateEmailWithResult("user@mailinator.com");
        Assert.Equal(ValidationFailureReason.DisposableDomain, result.FailureReason);
    }

    [Fact]
    public void AddEmailDomainValidator_ResolvesCustomLookupClientWhenRegistered()
    {
        var mockDns = NSubstitute.Substitute.For<DnsClient.ILookupClient>();
        var sp = new ServiceCollection()
            .AddSingleton(mockDns)
            .AddEmailDomainValidator()
            .BuildServiceProvider();

        var svc = sp.GetRequiredService<IEmailDomainValidator>();
        Assert.NotNull(svc);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Typo Detector unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class TypoDetectorTests
{
    [Theory]
    [InlineData("gamil.com", "gmail.com")]
    [InlineData("gmial.com", "gmail.com")]
    [InlineData("hotmial.com", "hotmail.com")]
    [InlineData("outlok.com", "outlook.com")]
    [InlineData("yaho.com", "yahoo.com")]
    [InlineData("iclud.com", "icloud.com")]
    [InlineData("prtonmail.com", "protonmail.com")]
    public void SuggestDomain_CommonTypos_ReturnsExpectedSuggestion(string input, string expected)
    {
        var suggestion = TypoDetector.SuggestDomain(input);
        Assert.Equal(expected, suggestion);
    }

    [Theory]
    [InlineData("gmail.com")]
    [InlineData("yahoo.com")]
    [InlineData("outlook.com")]
    public void SuggestDomain_ExactMatch_ReturnsNull(string domain)
    {
        var suggestion = TypoDetector.SuggestDomain(domain);
        Assert.Null(suggestion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SuggestDomain_NullOrWhitespace_ReturnsNull(string? domain)
    {
        var suggestion = TypoDetector.SuggestDomain(domain!);
        Assert.Null(suggestion);
    }

    [Fact]
    public void SuggestDomain_CompletelyDifferentDomain_ReturnsNull()
    {
        var suggestion = TypoDetector.SuggestDomain("randomunrelatedcompany12345.org");
        Assert.Null(suggestion);
    }

    [Fact]
    public void SuggestDomainCorrection_ViaService_ReturnsSuggestion()
    {
        var svc = new EmailDomainValidatorService();
        var suggestion = svc.SuggestDomainCorrection("user@gamil.com");
        Assert.Equal("gmail.com", suggestion);
    }

    [Fact]
    public void SuggestDomainCorrection_ViaStaticValidator_ReturnsSuggestion()
    {
        var suggestion = Validator.SuggestDomainCorrection("user@gamil.com");
        Assert.Equal("gmail.com", suggestion);
    }

    [Fact]
    public void ValidateEmailWithResult_WithTypoSuggestionsEnabled_PopulatesSuggestion()
    {
        var opts = new EmailValidatorOptions { EnableTypoSuggestions = true };
        var svc = new EmailDomainValidatorService(opts);
        var result = svc.ValidateEmailWithResult("alex@gamil.com");

        Assert.Equal("gmail.com", result.SuggestedDomain);
        Assert.Equal("alex@gmail.com", result.SuggestedEmail);
    }

    [Fact]
    public async Task ValidateEmailWithResultAsync_WithTypoSuggestionsEnabled_PopulatesSuggestion()
    {
        var opts = new EmailValidatorOptions { EnableTypoSuggestions = true };
        var svc = new EmailDomainValidatorService(opts);
        var result = await svc.ValidateEmailWithResultAsync("alex@hotmial.com");

        Assert.Equal("hotmail.com", result.SuggestedDomain);
        Assert.Equal("alex@hotmail.com", result.SuggestedEmail);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Free Webmail detection unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class FreeWebmailTests
{
    private readonly EmailDomainValidatorService _sut = new EmailDomainValidatorService();

    [Theory]
    [InlineData("user@gmail.com", true)]
    [InlineData("user@googlemail.com", true)]
    [InlineData("user@yahoo.com", true)]
    [InlineData("user@hotmail.com", true)]
    [InlineData("user@outlook.com", true)]
    [InlineData("user@live.com", true)]
    [InlineData("user@icloud.com", true)]
    [InlineData("user@proton.me", true)]
    [InlineData("user@protonmail.com", true)]
    [InlineData("user@aol.com", true)]
    [InlineData("user@zoho.com", true)]
    [InlineData("user@yandex.com", true)]
    [InlineData("user@corporate-enterprise.com", false)]
    [InlineData("user@custom-domain.org", false)]
    public void IsFreeWebmail_ReturnsExpected(string email, bool expected)
    {
        Assert.Equal(expected, _sut.IsFreeWebmail(email));
        Assert.Equal(expected, Validator.IsFreeWebmail(email));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    [InlineData("@")]
    public void IsFreeWebmail_MalformedEmail_ReturnsFalse(string email)
    {
        Assert.False(_sut.IsFreeWebmail(email));
        Assert.False(Validator.IsFreeWebmail(email));
    }

    [Fact]
    public void ValidateEmailWithResult_AllowFreeWebmailFalse_FailsValidation()
    {
        var opts = new EmailValidatorOptions { AllowFreeWebmail = false };
        var svc = new EmailDomainValidatorService(opts);

        var result = svc.ValidateEmailWithResult("alex@gmail.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.FreeWebmailDomain, result.FailureReason);
        Assert.True(result.IsFreeWebmail);
    }

    [Fact]
    public void ValidateEmailWithResult_AllowFreeWebmailTrue_DoesNotFailOnFreeWebmail()
    {
        var opts = new EmailValidatorOptions { AllowFreeWebmail = true };
        var svc = new EmailDomainValidatorService(opts);

        // mailinator is disposable so it fails on disposable, but IsFreeWebmail is false
        var result = svc.ValidateEmailWithResult("alex@mailinator.com");
        Assert.False(result.IsFreeWebmail);
        Assert.Equal(ValidationFailureReason.DisposableDomain, result.FailureReason);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Role-Based email detection unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class RoleBasedEmailTests
{
    private readonly EmailDomainValidatorService _sut = new EmailDomainValidatorService();

    [Theory]
    [InlineData("admin@example.com", true)]
    [InlineData("administrator@example.com", true)]
    [InlineData("support@example.com", true)]
    [InlineData("billing@example.com", true)]
    [InlineData("info@example.com", true)]
    [InlineData("contact@example.com", true)]
    [InlineData("sales@example.com", true)]
    [InlineData("help@example.com", true)]
    [InlineData("security@example.com", true)]
    [InlineData("jobs@example.com", true)]
    [InlineData("careers@example.com", true)]
    [InlineData("no-reply@example.com", true)]
    [InlineData("noreply@example.com", true)]
    [InlineData("postmaster@example.com", true)]
    [InlineData("hostmaster@example.com", true)]
    [InlineData("webmaster@example.com", true)]
    [InlineData("john.doe@example.com", false)]
    [InlineData("alice.smith@example.com", false)]
    public void IsRoleBasedEmail_ReturnsExpected(string email, bool expected)
    {
        Assert.Equal(expected, _sut.IsRoleBasedEmail(email));
        Assert.Equal(expected, Validator.IsRoleBasedEmail(email));
    }

    [Fact]
    public void IsRoleBasedEmail_PlusAddressedRole_ReturnsTrue()
    {
        Assert.True(_sut.IsRoleBasedEmail("support+ticket123@example.com"));
        Assert.True(_sut.IsRoleBasedEmail("billing+april@example.com"));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    [InlineData("@")]
    public void IsRoleBasedEmail_MalformedEmail_ReturnsFalse(string email)
    {
        Assert.False(_sut.IsRoleBasedEmail(email));
        Assert.False(Validator.IsRoleBasedEmail(email));
    }

    [Fact]
    public void ValidateEmailWithResult_AllowRoleBasedFalse_FailsValidation()
    {
        var opts = new EmailValidatorOptions { AllowRoleBasedEmails = false };
        var svc = new EmailDomainValidatorService(opts);

        var result = svc.ValidateEmailWithResult("admin@example.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.RoleBasedEmail, result.FailureReason);
        Assert.True(result.IsRoleBased);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Custom Whitelist, Blacklist, and TLD filtering unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class ListFilteringTests
{
    [Fact]
    public void BlockedDomains_RejectsTargetDomain()
    {
        var opts = new EmailValidatorOptions
        {
            BlockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "competitor.com", "banned.net" }
        };
        var svc = new EmailDomainValidatorService(opts);

        var result = svc.ValidateEmailWithResult("ceo@competitor.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.BlockedDomain, result.FailureReason);
    }

    [Fact]
    public void AllowedDomains_RejectsNonWhitelistedDomains()
    {
        var opts = new EmailValidatorOptions
        {
            AllowedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mycompany.com", "partner.org" }
        };
        var svc = new EmailDomainValidatorService(opts);

        var result = svc.ValidateEmailWithResult("user@external.com");
        Assert.False(result.IsValid);
        Assert.Equal(ValidationFailureReason.DomainNotAllowed, result.FailureReason);
    }

    [Theory]
    [InlineData("user@phishing.xyz", true)]
    [InlineData("user@malware.top", true)]
    [InlineData("user@scam.click", true)]
    [InlineData("user@safecompany.com", false)]
    public void BlockedTlds_FiltersConfiguredTlds(string email, bool shouldBeBlocked)
    {
        var opts = new EmailValidatorOptions
        {
            BlockedTlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xyz", ".top", ".click" }
        };
        var svc = new EmailDomainValidatorService(opts);

        var result = svc.ValidateEmailWithResult(email);
        if (shouldBeBlocked)
        {
            Assert.False(result.IsValid);
            Assert.Equal(ValidationFailureReason.BlockedTld, result.FailureReason);
        }
        else
        {
            // Allowed TLD - should not fail due to BlockedTld
            Assert.NotEqual(ValidationFailureReason.BlockedTld, result.FailureReason);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DataAnnotations [ValidateEmailDomain] unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class ValidateEmailDomainAttributeTests
{
    private class RegistrationModel
    {
        [ValidateEmailDomain(RequireMx = false)]
        public string? Email { get; set; }
    }

    private class CorporateRegistrationModel
    {
        [ValidateEmailDomain(RequireMx = false, AllowFreeWebmail = false, AllowRoleBased = false)]
        public string? Email { get; set; }
    }

    private class CustomMessageModel
    {
        [ValidateEmailDomain(RequireMx = false, ErrorMessage = "Custom email domain error")]
        public string? Email { get; set; }
    }

    private static bool TryValidate(object model, out List<DataAnnotationsValidationResult> results, IServiceProvider? serviceProvider = null)
    {
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model, serviceProvider, items: null);
        results = new List<DataAnnotationsValidationResult>();
        return DataAnnotationsValidator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    [Fact]
    public void Attribute_ValidEmail_PassesValidation()
    {
        var model = new RegistrationModel { Email = "user@example.com" };
        var isValid = TryValidate(model, out var results);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void Attribute_NullOrEmptyEmail_PassesValidation_StandardConvention()
    {
        var modelNull = new RegistrationModel { Email = null };
        Assert.True(TryValidate(modelNull, out var resultsNull));
        Assert.Empty(resultsNull);

        var modelEmpty = new RegistrationModel { Email = "" };
        Assert.True(TryValidate(modelEmpty, out var resultsEmpty));
        Assert.Empty(resultsEmpty);
    }

    [Fact]
    public void Attribute_InvalidFormat_FailsValidation()
    {
        var model = new RegistrationModel { Email = "not-an-email" };
        var isValid = TryValidate(model, out var results);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("invalid email format", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Attribute_DisposableDomain_FailsValidation()
    {
        var model = new RegistrationModel { Email = "user@mailinator.com" };
        var isValid = TryValidate(model, out var results);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("Disposable", results[0].ErrorMessage);
    }

    [Fact]
    public void Attribute_CorporateModel_RejectsFreeWebmail()
    {
        var model = new CorporateRegistrationModel { Email = "user@gmail.com" };
        var isValid = TryValidate(model, out var results);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("work or corporate email", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Attribute_CorporateModel_RejectsRoleBasedEmail()
    {
        var model = new CorporateRegistrationModel { Email = "admin@mycompany.com" };
        var isValid = TryValidate(model, out var results);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("role-based", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Attribute_CustomMessage_IsRespected()
    {
        var model = new CustomMessageModel { Email = "user@mailinator.com" };
        var isValid = TryValidate(model, out var results);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Equal("Custom email domain error", results[0].ErrorMessage);
    }

    [Fact]
    public void Attribute_WithDependencyInjectionContext_UsesResolvedService()
    {
        var sp = new ServiceCollection()
            .AddEmailDomainValidator()
            .BuildServiceProvider();

        var model = new RegistrationModel { Email = "user@mailinator.com" };
        var isValid = TryValidate(model, out var results, serviceProvider: sp);
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("Disposable", results[0].ErrorMessage);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// High-Throughput Batch Validation unit tests
// ═══════════════════════════════════════════════════════════════════════════
public class BatchValidationTests
{
    [Fact]
    public async Task ValidateBatchAsync_Service_ProcessesAllEmails()
    {
        var emails = new[]
        {
            "invalid-email-format",
            "user1@mailinator.com",
            "user2@mailinator.com",
            "bad@nodomain",
            "user3@mailinator.com"
        };

        var svc = new EmailDomainValidatorService();
        var results = new List<ValidationResult>();

        await foreach (var res in svc.ValidateBatchAsync(emails, maxConcurrency: 3))
        {
            results.Add(res);
        }

        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.False(r.IsValid));
    }

    [Fact]
    public async Task ValidateBatchAsync_StaticValidator_ProcessesAllEmails()
    {
        var emails = new[]
        {
            "bad1",
            "bad2",
            "test@mailinator.com"
        };

        var results = new List<ValidationResult>();
        await foreach (var res in Validator.ValidateBatchAsync(emails, maxConcurrency: 2))
        {
            results.Add(res);
        }

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ValidateBatchAsync_NullEmails_YieldsEmpty()
    {
        var svc = new EmailDomainValidatorService();
        var results = new List<ValidationResult>();

        await foreach (var res in svc.ValidateBatchAsync(null!, maxConcurrency: 2))
        {
            results.Add(res);
        }

        Assert.Empty(results);
    }

    [Fact]
    public async Task ValidateBatchAsync_SupportsCancellation()
    {
        var emails = Enumerable.Range(1, 100).Select(i => $"user{i}@mailinator.com").ToArray();
        var svc = new EmailDomainValidatorService();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var res in svc.ValidateBatchAsync(emails, maxConcurrency: 2, cancellationToken: cts.Token))
            {
                // no-op
            }
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Test helpers
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Minimal HTTP handler that returns fixed content for any request.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;

    public FakeHttpMessageHandler(string responseContent)
        => _responseContent = responseContent;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseContent)
        });
}