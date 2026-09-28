using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace EmailDomainValidator
{
    /// <summary>
    /// Validation attribute to validate that a property contains an email address with a valid format,
    /// a non-disposable domain, and resolvable MX records.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidateEmailDomainAttribute : ValidationAttribute
    {
        /// <summary>Whether to check for resolvable DNS MX records. Defaults to true.</summary>
        public bool RequireMx { get; set; } = true;

        /// <summary>Whether disposable email domains are permitted. Defaults to false.</summary>
        public bool AllowDisposable { get; set; } = false;

        /// <summary>Whether consumer free webmail addresses (e.g. @gmail.com) are permitted. Defaults to true.</summary>
        public bool AllowFreeWebmail { get; set; } = true;

        /// <summary>Whether role-based email addresses (e.g. admin@, info@) are permitted. Defaults to true.</summary>
        public bool AllowRoleBased { get; set; } = true;

        public ValidateEmailDomainAttribute()
        {
        }

        public ValidateEmailDomainAttribute(string errorMessage) : base(errorMessage)
        {
        }

        protected override System.ComponentModel.DataAnnotations.ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value is null)
                return System.ComponentModel.DataAnnotations.ValidationResult.Success;

            if (value is not string email || string.IsNullOrWhiteSpace(email))
                return System.ComponentModel.DataAnnotations.ValidationResult.Success; // Standard DataAnnotations convention: [Required] handles empty checks

            var validator = validationContext?.GetService<IEmailDomainValidator>();

            if (validator != null)
            {
                if (!validator.IsValidFormat(email))
                    return new System.ComponentModel.DataAnnotations.ValidationResult(
                        ErrorMessage ?? $"The field {validationContext?.DisplayName ?? "Email"} has an invalid email format.");

                if (!AllowDisposable && validator.IsDisposableEmail(email))
                    return new System.ComponentModel.DataAnnotations.ValidationResult(
                        ErrorMessage ?? "Disposable email addresses are not allowed.");

                if (!AllowFreeWebmail && validator.IsFreeWebmail(email))
                    return new System.ComponentModel.DataAnnotations.ValidationResult(
                        ErrorMessage ?? "Please provide a work or corporate email address.");

                if (!AllowRoleBased && validator.IsRoleBasedEmail(email))
                    return new System.ComponentModel.DataAnnotations.ValidationResult(
                        ErrorMessage ?? "Generic or role-based email addresses are not allowed.");

                if (RequireMx && !validator.HasValidMxRecords(email))
                    return new System.ComponentModel.DataAnnotations.ValidationResult(
                        ErrorMessage ?? $"The email domain has no valid mail exchanger (MX) records.");

                return System.ComponentModel.DataAnnotations.ValidationResult.Success;
            }

            // Fallback to static facade if DI is unavailable
            if (!EmailValidator.IsValidFormat(email))
                return new System.ComponentModel.DataAnnotations.ValidationResult(
                    ErrorMessage ?? $"The field {validationContext?.DisplayName ?? "Email"} has an invalid email format.");

            if (!AllowDisposable && EmailValidator.IsDisposableEmail(email))
                return new System.ComponentModel.DataAnnotations.ValidationResult(
                    ErrorMessage ?? "Disposable email addresses are not allowed.");

            if (!AllowFreeWebmail && EmailValidator.IsFreeWebmail(email))
                return new System.ComponentModel.DataAnnotations.ValidationResult(
                    ErrorMessage ?? "Please provide a work or corporate email address.");

            if (!AllowRoleBased && EmailValidator.IsRoleBasedEmail(email))
                return new System.ComponentModel.DataAnnotations.ValidationResult(
                    ErrorMessage ?? "Generic or role-based email addresses are not allowed.");

            if (RequireMx && !EmailValidator.HasValidMxRecords(email))
                return new System.ComponentModel.DataAnnotations.ValidationResult(
                    ErrorMessage ?? $"The email domain has no valid mail exchanger (MX) records.");

            return System.ComponentModel.DataAnnotations.ValidationResult.Success;
        }
    }
}
