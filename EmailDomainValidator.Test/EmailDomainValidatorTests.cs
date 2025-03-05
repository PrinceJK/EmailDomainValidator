namespace EmailDomainValidator.Test;

public class EmailDomainValidatorTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("test@mailinator.com", false)]
    [InlineData("invalid-email", false)]
    public void ValidateEmail_ShouldReturnExpectedResult(string email, bool expected)
    {
        var result = EmailDomainValidator.ValidateEmail(email);
        Assert.Equal(expected, result);
    }
}