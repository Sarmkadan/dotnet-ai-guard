namespace AiGuard.Tests;

public class LuhnValidatorTests
{
    [Theory]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("5500-0000-0000-0004")]
    [InlineData("378282246310005")]
    public void IsValid_ValidNumber_ReturnsTrue(string input)
    {
        Assert.True(LuhnValidator.IsValid(input));
    }

    [Theory]
    [InlineData("4111111111111112")]
    [InlineData("1234567812345678")]
    [InlineData("")]
    [InlineData("4111 1111 1111 111a")]
    [InlineData("12345")]
    [InlineData("12345678901234567890")]
    public void IsValid_InvalidNumber_ReturnsFalse(string input)
    {
        Assert.False(LuhnValidator.IsValid(input));
    }
}
