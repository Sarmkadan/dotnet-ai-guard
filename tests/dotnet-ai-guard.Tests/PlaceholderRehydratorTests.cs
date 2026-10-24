namespace AiGuard.Tests;

public class PlaceholderRehydratorTests
{
    private const string Original = "Email me at a@b.com and call 555-123-4567";

    private static RegexPiiRedactor CreateRedactor()
    {
        var monitor = new TestOptionsMonitor<AiGuardOptions>(new AiGuardOptions { RedactCategories = PiiCategory.All });
        return new RegexPiiRedactor(monitor);
    }

    [Fact]
    public async Task Rehydrate_RedactedOriginal_RestoresOriginalText()
    {
        var result = await CreateRedactor().RedactAsync(Original);

        var rehydrated = PlaceholderRehydrator.Rehydrate(result.RedactedText, Original, result);

        Assert.Equal(Original, rehydrated);
    }

    [Fact]
    public async Task Rehydrate_LlmStyleOutput_ContainsOriginalEmail()
    {
        var result = await CreateRedactor().RedactAsync(Original);

        var rehydrated = PlaceholderRehydrator.Rehydrate("Sure, I will write to [EMAIL_1] tomorrow", Original, result);

        Assert.Contains("a@b.com", rehydrated);
    }

    [Fact]
    public async Task Rehydrate_UnknownToken_LeavesTokenUnchanged()
    {
        var result = await CreateRedactor().RedactAsync(Original);

        var rehydrated = PlaceholderRehydrator.Rehydrate("Value: [FOO_9]", Original, result);

        Assert.Equal("Value: [FOO_9]", rehydrated);
    }

    [Fact]
    public async Task Rehydrate_OutputWithoutPlaceholders_ReturnsInputUnchanged()
    {
        var result = await CreateRedactor().RedactAsync(Original);
        const string output = "Nothing to restore here.";

        var rehydrated = PlaceholderRehydrator.Rehydrate(output, Original, result);

        Assert.Equal(output, rehydrated);
    }

    [Fact]
    public async Task Rehydrate_RepeatedPlaceholder_ReplacesEveryOccurrence()
    {
        var result = await CreateRedactor().RedactAsync(Original);

        var rehydrated = PlaceholderRehydrator.Rehydrate("[EMAIL_1] and again [EMAIL_1]", Original, result);

        Assert.Equal("a@b.com and again a@b.com", rehydrated);
    }
}
