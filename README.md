# EmailDomainValidator

A lightweight, free .NET library for validating email addresses — checking format, detecting disposable domains, and verifying real MX records.

---

## Key Features

- **Email Format Validation** — regex-based format check (`user@domain.com`)
- **Disposable Email Detection** — blocks known throwaway domains (5,400+ entries, embedded at build time)
- **Real MX Record Verification** — uses actual DNS MX queries via [DnsClient.NET](https://github.com/MichaCo/DnsClient.NET), not just A-record resolution
- **Rich Validation Results** — `ValidationResult` tells you *why* an email failed (`InvalidFormat`, `DisposableDomain`, `NoMxRecords`)
- **Dependency Injection Support** — register via `services.AddEmailDomainValidator()` or use the static API
- **Configurable Options** — tune cache TTL and blocklist update URL via `EmailValidatorOptions`
- **Blocklist Update Mechanism** — refresh the in-memory blocklist at runtime from any URL, no restart required
- **Async Support** — every method has an async counterpart
- **MX Result Caching** — DNS lookups are cached (default: 1 hour) to avoid redundant queries

---

## Installation

```bash
dotnet add package EmailDomainValidator
```

---

## Quick Start

### Static API

```csharp
using EmailDomainValidator;

// Simple bool check
bool isValid = EmailDomainValidator.ValidateEmail("user@example.com");

// Async
bool isValid = await EmailDomainValidator.ValidateEmailAsync("user@example.com");

// Detailed result — know exactly why validation failed
ValidationResult result = EmailDomainValidator.ValidateEmailWithResult("user@mailinator.com");
// result.IsValid        -> false
// result.FailureReason  -> ValidationFailureReason.DisposableDomain

// Implicit bool conversion
if (!result) Console.WriteLine($"Rejected: {result.FailureReason}");
```

### Dependency Injection (ASP.NET Core / Generic Host)

```csharp
// Program.cs
builder.Services.AddEmailDomainValidator();

// Or with custom options
builder.Services.AddEmailDomainValidator(options =>
{
    options.CacheTtl = TimeSpan.FromMinutes(30);
});
```

```csharp
// In a controller, service, etc.
public class RegistrationService(IEmailDomainValidator validator)
{
    public async Task RegisterAsync(string email)
    {
        var result = await validator.ValidateEmailWithResultAsync(email);
        if (!result)
            throw new ArgumentException($"Invalid email: {result.FailureReason}");
        // ...
    }
}
```

---

## API Reference

### `EmailDomainValidator` (static class) / `IEmailDomainValidator` (interface)

| Method | Returns | Description |
|---|---|---|
| `IsValidFormat(email)` | `bool` | Regex format check |
| `IsDisposableEmail(email)` | `bool` | Checks against embedded blocklist |
| `HasValidMxRecords(email)` | `bool` | Real DNS MX query (sync) |
| `HasValidMxRecordsAsync(email)` | `Task<bool>` | Real DNS MX query (async) |
| `ValidateEmail(email)` | `bool` | All checks combined (sync) |
| `ValidateEmailAsync(email)` | `Task<bool>` | All checks combined (async) |
| `ValidateEmailWithResult(email)` | `ValidationResult` | All checks, with failure reason (sync) |
| `ValidateEmailWithResultAsync(email)` | `Task<ValidationResult>` | All checks, with failure reason (async) |
| `UpdateBlocklistAsync(url)` | `Task` | Fetch a fresh blocklist from a URL at runtime |

### `ValidationResult`

```csharp
result.IsValid        // bool
result.FailureReason  // ValidationFailureReason enum
(bool)result          // implicit conversion
result.ToString()     // "Valid" or "Invalid: DisposableDomain"
```

`ValidationFailureReason` values: `None`, `InvalidFormat`, `DisposableDomain`, `NoMxRecords`

### `EmailValidatorOptions`

```csharp
new EmailValidatorOptions
{
    CacheTtl = TimeSpan.FromHours(1),   // how long MX results are cached
    BlocklistUpdateUrl = null            // optional URL for runtime blocklist refresh
}
```

---

## Disposable Email Blocklist

The embedded blocklist ships with **5,400+ known disposable domains** and is sourced from the community-maintained
[disposable-email-domains](https://github.com/disposable-email-domains/disposable-email-domains) project.

Since new disposable providers appear regularly, the library provides a runtime update mechanism so your application
can refresh the list without a redeployment:

```csharp
// Refresh at startup or on a schedule
await EmailDomainValidator.UpdateBlocklistAsync(
    "https://raw.githubusercontent.com/disposable-email-domains/disposable-email-domains/master/disposable_email_blocklist.conf"
);
```

The new list is swapped in atomically — no downtime, no restart.

---

## Dependencies

| Package | Purpose |
|---|---|
| `DnsClient` | Real DNS MX record queries |
| `Microsoft.Extensions.Caching.Memory` | MX result caching |
| `Microsoft.Extensions.DependencyInjection` | DI registration support |
| `Microsoft.Extensions.Http` | `HttpClient` for blocklist updates |

---

## Use Cases

- **User Registration** — block fake/throwaway accounts at sign-up
- **Newsletter Subscriptions** — keep mailing lists clean
- **E-commerce** — reduce fraud and improve delivery rates
- **Lead Generation** — validate form submissions before they hit your CRM
- **Data Cleaning** — validate existing email datasets in bulk

---

## Contributing

Contributions are welcome. Please fork the repository, make your changes on a feature branch, and open a pull request.
For significant changes, open an issue first.

---

## License

MIT — see [LICENSE](LICENSE) for details.

---

## Links

- [NuGet Package](https://www.nuget.org/packages/EmailDomainValidator)
- [GitHub Repository](https://github.com/PrinceJK/EmailDomainValidator)
- [Upstream Blocklist](https://github.com/disposable-email-domains/disposable-email-domains)