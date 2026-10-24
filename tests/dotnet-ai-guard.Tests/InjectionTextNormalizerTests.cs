namespace AiGuard.Tests;

public class InjectionTextNormalizerTests
{
    [Theory]
    [InlineData("ｉｇｎｏｒｅ", "ignore")]
    [InlineData("​ignore‌ ‍all﻿", "ignore all")]
    [InlineData("иgnore", "иgnore")]
    [InlineData("ignоre аll", "ignore all")]
    [InlineData("Рессу", "Peccу")]
    [InlineData("a\t\n  b  c", "a b c")]
    [InlineData("  already clean  ", " already clean ")]
    public void Normalize_Obfuscated_ReturnsCanonicalText(string input, string expected)
    {
        Assert.Equal(expected, InjectionTextNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InjectionTextNormalizer.Normalize(string.Empty));
    }

    [Fact]
    public void Normalize_ZeroWidthBetweenSpaces_CollapsesToSingleSpace()
    {
        Assert.Equal("a b", InjectionTextNormalizer.Normalize("a ​ b"));
    }

    [Fact]
    public void Normalize_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => InjectionTextNormalizer.Normalize(null!));
    }
}
