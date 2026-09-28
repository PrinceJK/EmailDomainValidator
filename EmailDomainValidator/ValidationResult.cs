namespace EmailDomainValidator
{
    public enum ValidationFailureReason
    {
        None,
        InvalidFormat,
        DisposableDomain,
        NoMxRecords,
        BlockedDomain,
        DomainNotAllowed,
        BlockedTld,
        FreeWebmailDomain,
        RoleBasedEmail
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public ValidationFailureReason FailureReason { get; }
        public string? Email { get; }
        public string? SuggestedEmail { get; }
        public string? SuggestedDomain { get; }
        public bool IsDisposable { get; }
        public bool IsFreeWebmail { get; }
        public bool IsRoleBased { get; }

        public ValidationResult(
            bool isValid,
            ValidationFailureReason reason,
            string? email = null,
            string? suggestedEmail = null,
            string? suggestedDomain = null,
            bool isDisposable = false,
            bool isFreeWebmail = false,
            bool isRoleBased = false)
        {
            IsValid = isValid;
            FailureReason = reason;
            Email = email;
            SuggestedEmail = suggestedEmail;
            SuggestedDomain = suggestedDomain;
            IsDisposable = isDisposable;
            IsFreeWebmail = isFreeWebmail;
            IsRoleBased = isRoleBased;
        }

        public static ValidationResult Success(
            string? email = null,
            string? suggestedEmail = null,
            string? suggestedDomain = null,
            bool isFreeWebmail = false,
            bool isRoleBased = false) =>
            new ValidationResult(
                true,
                ValidationFailureReason.None,
                email,
                suggestedEmail,
                suggestedDomain,
                isDisposable: false,
                isFreeWebmail: isFreeWebmail,
                isRoleBased: isRoleBased);

        public static ValidationResult Fail(
            ValidationFailureReason reason,
            string? email = null,
            string? suggestedEmail = null,
            string? suggestedDomain = null,
            bool isDisposable = false,
            bool isFreeWebmail = false,
            bool isRoleBased = false) =>
            new ValidationResult(
                false,
                reason,
                email,
                suggestedEmail,
                suggestedDomain,
                isDisposable: isDisposable,
                isFreeWebmail: isFreeWebmail,
                isRoleBased: isRoleBased);

        public static implicit operator bool(ValidationResult result) => result.IsValid;

        public override string ToString()
        {
            if (IsValid)
            {
                return SuggestedEmail != null
                    ? $"Valid (Did you mean: {SuggestedEmail}?)"
                    : "Valid";
            }

            return SuggestedEmail != null
                ? $"Invalid: {FailureReason} (Did you mean: {SuggestedEmail}?)"
                : $"Invalid: {FailureReason}";
        }
    }
}
