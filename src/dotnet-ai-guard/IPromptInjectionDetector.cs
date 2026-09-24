namespace AiGuard;

/// <summary>
/// Analyzes free-form text (user prompts, tool outputs, retrieved documents) for
/// prompt-injection attempts such as instruction overrides, role hijacking,
/// system-prompt exfiltration and delimiter smuggling.
/// </summary>
public interface IPromptInjectionDetector
{
    /// <summary>
    /// Scans <paramref name="text"/> and returns a verdict with a risk score and the matched signals.
    /// </summary>
    /// <param name="text">The text to analyze. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="InjectionVerdict"/> describing the outcome of the scan.</returns>
    ValueTask<InjectionVerdict> DetectAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a prompt-injection scan.
/// </summary>
/// <param name="IsInjection">Whether the score exceeded the configured threshold.</param>
/// <param name="Score">Risk score in the range [0, 1]. Higher means more likely to be an injection.</param>
/// <param name="Signals">Individual signals (rule identifiers) that contributed to the score.</param>
public sealed record InjectionVerdict(bool IsInjection, double Score, IReadOnlyList<InjectionSignal> Signals)
{
    /// <summary>A verdict representing clean input with no matched signals.</summary>
    public static InjectionVerdict Clean { get; } = new(false, 0d, Array.Empty<InjectionSignal>());
}

/// <summary>
/// A single detection signal produced by a rule or heuristic.
/// </summary>
/// <param name="RuleId">Stable identifier of the rule, e.g. <c>INJ-001</c>.</param>
/// <param name="Description">Human readable explanation of what matched.</param>
/// <param name="Weight">Contribution of this signal to the overall score, in the range [0, 1].</param>
/// <param name="Span">Location of the match within the analyzed text, or <see langword="null"/> if not positional.</param>
public sealed record InjectionSignal(string RuleId, string Description, double Weight, TextSpan? Span);

/// <summary>
/// A half-open character range <c>[Start, Start + Length)</c> within a string.
/// </summary>
/// <param name="Start">Zero-based index of the first character.</param>
/// <param name="Length">Number of characters in the span.</param>
public readonly record struct TextSpan(int Start, int Length);
