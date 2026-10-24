namespace AiGuard.Tests;

public class IbanValidatorTests
{
    [Theory]
    [InlineData("DE89 3704 0044 0532 0130 00")]
    [InlineData("GB82WEST12345698765432")]
    [InlineData("FR1420041010050500013M02606")]
    [InlineData("NL91ABNA0417164300")]
    [InlineData("BG80BNBG96611020345678")]
    public void IsValid_ValidIban_ReturnsTrue(string input)
    {
        Assert.True(IbanValidator.IsValid(input));
    }

    [Theory]
    [InlineData("de89370400440532013000")]
    [InlineData("gb82 west 1234 5698 7654 32")]
    public void IsValid_LowercaseOrSpacedIban_ReturnsTrue(string input)
    {
        Assert.True(IbanValidator.IsValid(input));
    }

    [Theory]
    [InlineData("DE89 3704 0044 0532 0130 01")]
    [InlineData("GB82WEST12345698765433")]
    public void IsValid_BadChecksum_ReturnsFalse(string input)
    {
        Assert.False(IbanValidator.IsValid(input));
    }

    [Theory]
    [InlineData("DE893704004405320130")]
    [InlineData("NL91ABNA04171643001")]
    public void IsValid_WrongLengthForCountry_ReturnsFalse(string input)
    {
        Assert.False(IbanValidator.IsValid(input));
    }

    [Theory]
    [InlineData("XX00")]
    [InlineData("")]
    [InlineData("DE")]
    [InlineData("1234567890ABCDE")]
    [InlineData("DE89370400440532013!00")]
    [InlineData("DE8937040044053201300000000000000000000")]
    public void IsValid_MalformedInput_ReturnsFalse(string input)
    {
        Assert.False(IbanValidator.IsValid(input));
    }
}
