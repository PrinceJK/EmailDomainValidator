namespace EmailDomainValidator
{
    public enum ValidationFailureReason
    {
        None,
        InvalidFormat,
        DisposableDomain,
        NoMxRecords
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public ValidationFailureReason FailureReason { get; }

        private ValidationResult(bool isValid, ValidationFailureReason reason)
        {
            IsValid = isValid;
            FailureReason = reason;
        }

        public static ValidationResult Success() =>
            new ValidationResult(true, ValidationFailureReason.None);

        public static ValidationResult Fail(ValidationFailureReason reason) =>
            new ValidationResult(false, reason);

        public static implicit operator bool(ValidationResult result) => result.IsValid;

        public override string ToString() =>
            IsValid ? "Valid" : $"Invalid: {FailureReason}";
    }
}
