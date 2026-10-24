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
    /// <param name="extraRules">Additional rules run after <see cref="DefaultInjectionRules.All"/>. Optional.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public RuleBasedPromptInjectionDetector(IOptionsMonitor<AiGuardOptions> options, IEnumerable<InjectionRule>? extraRules = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _rules = extraRules is null
            ? DefaultInjectionRules.All
            : [.. DefaultInjectionRules.All, .. extraRules];
    }

    /// <summary>
    /// Scans <paramref name="text"/> for injection signals.
    /// </summary>
    /// <remarks>
    /// The text is first passed through <see cref="InjectionTextNormalizer.Normalize"/>, and the rules run
    /// over the normalized copy. Signal spans (<see cref="InjectionSignal.Span"/>) are therefore relative to the
    /// normalized text, not to <paramref name="text"/>; their positions can differ when the input contains
    /// zero-width characters, homoglyphs, compatibility forms or runs of whitespace.
    /// </remarks>
    /// <param name="text">The text to scan. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token, checked once before scanning.</param>
    /// <returns>The verdict. Its signals' spans index into the normalized text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>. Thrown synchronously, not via the returned task.</exception>
    public ValueTask<InjectionVerdict> DetectAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = InjectionTextNormalizer.Normalize(text);
        var signals = new List<InjectionSignal>();
        var complement = 1d;

        foreach (var rule in _rules)
        {
            foreach (var signal in rule.Match(normalized))
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
