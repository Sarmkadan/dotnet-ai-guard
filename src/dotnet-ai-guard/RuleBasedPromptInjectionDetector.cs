using Microsoft.Extensions.Options;

namespace AiGuard;

/// <summary>
/// Detects prompt injection by running a set of <see cref="InjectionRule"/>s over the text.
/// Every match contributes a signal; the signals are combined with a noisy-OR so that independent
/// weak indicators add up without the score ever exceeding 1.
/// </summary>
public sealed class RuleBasedPromptInjectionDetector : IPromptInjectionDetector
{
    private readonly IOptionsMonitor<AiGuardOptions> _options;
    private readonly IReadOnlyList<InjectionRule> _rules;

    /// <summary>
    /// Creates a new detector.
    /// </summary>
    /// <param name="options">Options monitor; <see cref="AiGuardOptions.InjectionThreshold"/> is read on every scan.</param>
    /// <param name="rules">Rules to run. Defaults to <see cref="DefaultInjectionRules.All"/>.</param>
    public RuleBasedPromptInjectionDetector(IOptionsMonitor<AiGuardOptions> options, IReadOnlyList<InjectionRule>? rules = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rules = rules ?? DefaultInjectionRules.All;
    }

    /// <inheritdoc />
    public ValueTask<InjectionVerdict> DetectAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var signals = new List<InjectionSignal>();
        var complement = 1d;

        foreach (var rule in _rules)
        {
            foreach (var signal in rule.Match(text))
            {
                signals.Add(signal);
                complement *= 1d - signal.Weight;
            }
        }

        if (signals.Count == 0)
        {
            return ValueTask.FromResult(InjectionVerdict.Clean);
        }

        // Noisy-OR: 1 - Π(1 - w). Clamped in case of floating-point drift.
        var score = Math.Clamp(1d - complement, 0d, 1d);
        var isInjection = score >= _options.CurrentValue.InjectionThreshold;

        return ValueTask.FromResult(new InjectionVerdict(isInjection, score, signals));
    }
}
