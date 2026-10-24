using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiGuard.Tests;

public class AiGuardServiceCollectionExtensionsTests
{
    private sealed class StubPiiRedactor : IPiiRedactor
    {
        public ValueTask<RedactionResult> RedactAsync(string text, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new RedactionResult(text, Array.Empty<PiiEntity>()));
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InjectionThreshold"] = "0.9",
                ["OnInjection"] = "Flag",
                ["RedactCategories"] = "Email, Phone",
            })
            .Build();

    [Fact]
    public void AddAiGuard_Defaults_ResolvesRuleBasedDetector()
    {
        using var provider = new ServiceCollection().AddLogging().AddAiGuard().BuildServiceProvider();

        Assert.IsType<RuleBasedPromptInjectionDetector>(provider.GetRequiredService<IPromptInjectionDetector>());
    }

    [Fact]
    public void AddAiGuard_Defaults_ResolvesRegexRedactor()
    {
        using var provider = new ServiceCollection().AddLogging().AddAiGuard().BuildServiceProvider();

        Assert.IsType<RegexPiiRedactor>(provider.GetRequiredService<IPiiRedactor>());
    }

    [Fact]
    public void AddAiGuard_ConfigureDelegate_AppliesInjectionThreshold()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAiGuard(o => o.InjectionThreshold = 0.5)
            .BuildServiceProvider();

        Assert.Equal(0.5, provider.GetRequiredService<IOptions<AiGuardOptions>>().Value.InjectionThreshold);
    }

    [Fact]
    public void AddAiGuard_Configuration_BindsInjectionThreshold()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAiGuard(CreateConfiguration())
            .BuildServiceProvider();

        Assert.Equal(0.9, provider.GetRequiredService<IOptions<AiGuardOptions>>().Value.InjectionThreshold);
    }

    [Fact]
    public void AddAiGuard_Configuration_BindsOnInjection()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAiGuard(CreateConfiguration())
            .BuildServiceProvider();

        Assert.Equal(GuardAction.Flag, provider.GetRequiredService<IOptions<AiGuardOptions>>().Value.OnInjection);
    }

    [Fact]
    public void AddAiGuard_Configuration_BindsRedactCategories()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAiGuard(CreateConfiguration())
            .BuildServiceProvider();

        Assert.Equal(
            PiiCategory.Email | PiiCategory.Phone,
            provider.GetRequiredService<IOptions<AiGuardOptions>>().Value.RedactCategories);
    }

    [Fact]
    public void AddAiGuard_CustomRedactorRegisteredFirst_KeepsCustomRedactor()
    {
        var custom = new StubPiiRedactor();
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPiiRedactor>(custom)
            .AddAiGuard()
            .BuildServiceProvider();

        Assert.Same(custom, provider.GetRequiredService<IPiiRedactor>());
    }
}
