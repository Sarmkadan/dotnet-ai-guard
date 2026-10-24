namespace AiGuard.Tests;

public class PiiGuardTests
{
    private const string Original = "Mail a@b.com now";

    private static TestOptionsMonitor<AiGuardOptions> CreateOptions() =>
        new(new AiGuardOptions { RedactCategories = PiiCategory.All });

    private static RedactionResult EmailResult() =>
        new(Original.Replace("a@b.com", "[EMAIL_1]"),
            new[] { new PiiEntity(PiiCategory.Email, new TextSpan(5, 7), "[EMAIL_1]") });

    [Fact]
    public void RegexPiiRedactorConstructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("options", () => new RegexPiiRedactor(null!));
    }

    [Fact]
    public async Task RegexPiiRedactorConstructor_EmptyExtraPatterns_KeepsDefaultPatterns()
    {
        var redactor = new RegexPiiRedactor(CreateOptions(), Array.Empty<PiiPattern>());

        var result = await redactor.RedactAsync("Mail a@b.com now");

        Assert.Equal("Mail [EMAIL_1] now", result.RedactedText);
    }

    [Fact]
    public async Task RegexPiiRedactorConstructor_NullExtraPatterns_UsesDefaultsWithoutThrowing()
    {
        var redactor = new RegexPiiRedactor(CreateOptions(), extraPatterns: null);

        var result = await redactor.RedactAsync("Mail a@b.com now");

        Assert.Equal("Mail [EMAIL_1] now", result.RedactedText);
    }

    [Fact]
    public void RedactAsync_NullText_ThrowsArgumentNullExceptionSynchronously()
    {
        var redactor = new RegexPiiRedactor(CreateOptions());

        Assert.Throws<ArgumentNullException>("text", () => Consume(redactor.RedactAsync(null!)));
    }

    [Fact]
    public void RehydrateNullRedactedOrLlmOutput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("redactedOrLlmOutput",
            () => PlaceholderRehydrator.Rehydrate(null!, Original, EmailResult()));
    }

    [Fact]
    public void Rehydrate_NullOriginalText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("originalText",
            () => PlaceholderRehydrator.Rehydrate("[EMAIL_1]", null!, EmailResult()));
    }

    [Fact]
    public void Rehydrate_NullResult_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("result",
            () => PlaceholderRehydrator.Rehydrate("[EMAIL_1]", Original, null!));
    }

    [Fact]
    public void Rehydrate_SpanExceedsOriginalLength_ThrowsArgumentOutOfRangeException()
    {
        var result = new RedactionResult("[EMAIL_1]",
            new[] { new PiiEntity(PiiCategory.Email, new TextSpan(5, 100), "[EMAIL_1]") });

        Assert.Throws<ArgumentOutOfRangeException>("result",
            () => PlaceholderRehydrator.Rehydrate("[EMAIL_1]", Original, result));
    }

    [Fact]
    public void Rehydrate_NegativeSpanStart_ThrowsArgumentOutOfRangeException()
    {
        var result = new RedactionResult("[EMAIL_1]",
            new[] { new PiiEntity(PiiCategory.Email, new TextSpan(-1, 3), "[EMAIL_1]") });

        Assert.Throws<ArgumentOutOfRangeException>("result",
            () => PlaceholderRehydrator.Rehydrate("[EMAIL_1]", Original, result));
    }

    [Fact]
    public void Rehydrate_NegativeSpanLength_ThrowsArgumentOutOfRangeException()
    {
        var result = new RedactionResult("[EMAIL_1]",
            new[] { new PiiEntity(PiiCategory.Email, new TextSpan(5, -2), "[EMAIL_1]") });

        Assert.Throws<ArgumentOutOfRangeException>("result",
            () => PlaceholderRehydrator.Rehydrate("[EMAIL_1]", Original, result));
    }

    [Fact]
    public void Rehydrate_SpanEndingExactlyAtTextEnd_DoesNotThrow()
    {
        var result = new RedactionResult("Mail [EMAIL_1]",
            new[] { new PiiEntity(PiiCategory.Email, new TextSpan(5, 7), "[EMAIL_1]") });

        var rehydrated = PlaceholderRehydrator.Rehydrate("Mail [EMAIL_1]", Original, result);

        Assert.Equal("Mail a@b.com", rehydrated);
    }

    [Fact]
    public void PiiPatternConstructor_ZeroCategory_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("category",
            () => new PiiPattern(PiiCategory.None, "x"));

        Assert.Equal("category", ex.ParamName);
    }

    [Fact]
    public void PiiPatternConstructor_MultipleFlags_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("category",
            () => new PiiPattern(PiiCategory.Email | PiiCategory.Phone, "x"));
    }

    [Fact]
    public void PiiPatternConstructor_AllFlags_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("category",
            () => new PiiPattern(PiiCategory.All, "x"));
    }

    [Fact]
    public void PiiPatternConstructor_SingleFlag_SetsCategory()
    {
        var pattern = new PiiPattern(PiiCategory.Email, "x");

        Assert.Equal(PiiCategory.Email, pattern.Category);
    }

    private static void Consume<T>(ValueTask<T> _)
    {
    }
}
