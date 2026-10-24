using Microsoft.Extensions.Options;

namespace AiGuard.Tests;

public class AiGuardOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(AiGuardOptions options) =>
        new AiGuardOptionsValidator().Validate(null, options);

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = Validate(new AiGuardOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_InjectionThresholdAboveOne_Fails()
    {
        var options = new AiGuardOptions { InjectionThreshold = 1.5 };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("InjectionThreshold", result.FailureMessage);
    }

    [Fact]
    public void Validate_MaxBodyBytesZero_Fails()
    {
        var options = new AiGuardOptions { MaxBodyBytes = 0 };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("MaxBodyBytes", result.FailureMessage);
    }

    [Fact]
    public void Validate_BlockedStatusCodeBelow400_Fails()
    {
        var options = new AiGuardOptions { BlockedStatusCode = 200 };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("BlockedStatusCode", result.FailureMessage);
    }

    [Fact]
    public void Validate_OnInjectionUndefinedValue_Fails()
    {
        var options = new AiGuardOptions { OnInjection = (GuardAction)99 };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("OnInjection", result.FailureMessage);
    }

    [Fact]
    public void Validate_PathPrefixNull_Fails()
    {
        var options = new AiGuardOptions();
        options.PathPrefixes.Add(null!);

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("PathPrefixes[0]", result.FailureMessage);
    }

    [Fact]
    public void Validate_PathPrefixWithoutLeadingSlash_Fails()
    {
        var options = new AiGuardOptions();
        options.PathPrefixes.Add("api/chat");

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains("PathPrefixes[0]", result.FailureMessage);
    }

    [Fact]
    public void Validate_MultipleViolations_ReportsEveryFailure()
    {
        var options = new AiGuardOptions
        {
            InjectionThreshold = -0.1,
            MaxBodyBytes = -1,
            BlockedStatusCode = 600,
            OnInjection = (GuardAction)42,
        };
        options.PathPrefixes.Add("  ");

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Equal(5, result.Failures.Count());
    }
}
