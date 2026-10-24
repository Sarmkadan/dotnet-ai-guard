using Microsoft.Extensions.Options;

namespace AiGuard;

/// <summary>
/// Validates <see cref="AiGuardOptions"/> so that misconfiguration fails at startup rather than on the first request.
/// Reports every violated rule in a single failure result.
/// </summary>
public sealed class AiGuardOptionsValidator : IValidateOptions<AiGuardOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AiGuardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (double.IsNaN(options.InjectionThreshold) || options.InjectionThreshold < 0d || options.InjectionThreshold > 1d)
        {
            failures.Add($"InjectionThreshold must be in the range [0, 1] but was {options.InjectionThreshold}.");
        }

        if (options.MaxBodyBytes <= 0)
        {
            failures.Add($"MaxBodyBytes must be greater than 0 but was {options.MaxBodyBytes}.");
        }

        if (options.BlockedStatusCode < 400 || options.BlockedStatusCode > 599)
        {
            failures.Add($"BlockedStatusCode must be in the range 400-599 but was {options.BlockedStatusCode}.");
        }

        if (!Enum.IsDefined(options.OnInjection))
        {
            failures.Add($"OnInjection must be a defined GuardAction value but was {(int)options.OnInjection}.");
        }

        for (var i = 0; i < options.PathPrefixes.Count; i++)
        {
            var prefix = options.PathPrefixes[i];
            if (string.IsNullOrWhiteSpace(prefix))
            {
                failures.Add($"PathPrefixes[{i}] must not be null or whitespace.");
            }
            else if (!prefix.StartsWith('/'))
            {
                failures.Add($"PathPrefixes[{i}] '{prefix}' must start with '/'.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
