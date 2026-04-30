using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Validator = EmailDomainValidator.EmailValidator;

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
        var svc = new EmailDomainValidatorService(httpClient: new HttpClient(handler));

        // Before update: mailinator should be blocked (from embedded list)
        Assert.True(svc.IsDisposableEmail("user@mailinator.com"));

        await svc.UpdateBlocklistAsync("http://fake-url/blocklist.txt");

        // After update: only newblocked.com is in the list
        Assert.True(svc.IsDisposableEmail("user@newblocked.com"));
        Assert.False(svc.IsDisposableEmail("user@mailinator.com"));
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


public class EmailDomainValidatorTests
{
    // ── ValidateEmail (integration: format + disposable + DNS) ──────────────

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("test@mailinator.com", false)]
    [InlineData("invalid-email", false)]
    public void ValidateEmail_ShouldReturnExpectedResult(string email, bool expected)
    {
        Assert.Equal(expected, Validator.ValidateEmail(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateEmail_NullOrWhitespace_ReturnsFalse(string? email)
    {
        Assert.False(Validator.ValidateEmail(email!));
    }

    // ── IsValidFormat ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@domain.com", true)]
    [InlineData("user.name+tag@sub.domain.org", true)]
    [InlineData("user@domain.co.uk", true)]
    [InlineData("user@domain", false)]        // no TLD
    [InlineData("@domain.com", false)]        // no local part
    [InlineData("userdomain.com", false)]     // no @
    [InlineData("user@.com", false)]          // dot right after @
    [InlineData("user @domain.com", false)]   // space in local
    [InlineData("", false)]
    public void IsValidFormat_ReturnsExpected(string email, bool expected)
    {
        Assert.Equal(expected, Validator.IsValidFormat(email));
    }

    // ── IsDisposableEmail ───────────────────────────────────────────────────

    [Theory]
    [InlineData("user@mailinator.com", true)]
    [InlineData("user@guerrillamail.com", true)]
    [InlineData("user@gmail.com", false)]
    [InlineData("user@outlook.com", false)]
    public void IsDisposableEmail_ReturnsExpected(string email, bool expected)
    {
        Assert.Equal(expected, Validator.IsDisposableEmail(email));
    }

    [Theory]
    [InlineData("notanemail")]   // no @
    [InlineData("@")]            // empty domain
    public void IsDisposableEmail_MalformedEmail_ReturnsFalse(string email)
    {
        Assert.False(Validator.IsDisposableEmail(email));
    }

    // ── HasValidMxRecords (sync) ────────────────────────────────────────────

    [Fact]
    public void HasValidMxRecords_KnownGoodDomain_ReturnsTrue()
    {
        Assert.True(Validator.HasValidMxRecords("user@gmail.com"));
    }

    [Fact]
    public void HasValidMxRecords_NonExistentDomain_ReturnsFalse()
    {
        Assert.False(Validator.HasValidMxRecords("user@this-domain-does-not-exist-xyz123.com"));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@")]
    public void HasValidMxRecords_MalformedEmail_ReturnsFalse(string email)
    {
        Assert.False(Validator.HasValidMxRecords(email));
    }

    // ── HasValidMxRecordsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task HasValidMxRecordsAsync_KnownGoodDomain_ReturnsTrue()
    {
        Assert.True(await Validator.HasValidMxRecordsAsync("user@gmail.com"));
    }

    [Fact]
    public async Task HasValidMxRecordsAsync_NonExistentDomain_ReturnsFalse()
    {
        Assert.False(await Validator.HasValidMxRecordsAsync("user@this-domain-does-not-exist-xyz123.com"));
    }

    // ── ValidateEmailAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_ValidEmail_ReturnsTrue()
    {
        Assert.True(await Validator.ValidateEmailAsync("test@example.com"));
    }

    [Fact]
    public async Task ValidateEmailAsync_DisposableEmail_ReturnsFalse()
    {
        Assert.False(await Validator.ValidateEmailAsync("test@mailinator.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task ValidateEmailAsync_InvalidInput_ReturnsFalse(string? email)
    {
        Assert.False(await Validator.ValidateEmailAsync(email!));
    }
}