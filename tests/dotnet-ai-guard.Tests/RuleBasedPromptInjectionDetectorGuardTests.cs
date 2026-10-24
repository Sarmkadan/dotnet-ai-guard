namespace AiGuard.Tests;

public class RuleBasedPromptInjectionDetectorGuardTests
{
    private static TestOptionsMonitor<AiGuardOptions> Options() => new(new AiGuardOptions());

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("options", () => new RuleBasedPromptInjectionDetector(null!));
    }

    [Fact]
    public void Constructor_EmptyRules_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("rules", () =>
            new RuleBasedPromptInjectionDetector(Options(), Array.Empty<InjectionRule>()));

        Assert.Equal("rules", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullRules_UsesDefaultRules()
    {
        var detector = new RuleBasedPromptInjectionDetector(Options(), rules: null);

        Assert.NotNull(detector);
    }

    [Fact]
    public void DetectAsync_NullText_ThrowsArgumentNullExceptionSynchronously()
    {
        var detector = new RuleBasedPromptInjectionDetector(Options());

        // The exception is thrown before a ValueTask exists, so Consume only exists to satisfy CA2012.
        Assert.Throws<ArgumentNullException>("text", () => Consume(detector.DetectAsync(null!)));
    }

    [Fact]
    public void Match_NullText_ThrowsArgumentNullExceptionSynchronously()
    {
        var rule = new InjectionRule("INJ-900", "Test rule", 0.5d, "ignore");

        Assert.Throws<ArgumentNullException>("text", () => rule.Match(null!));
    }

    private static void Consume<T>(ValueTask<T> _)
    {
    }
}
