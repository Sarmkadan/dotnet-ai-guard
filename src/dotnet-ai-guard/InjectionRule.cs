using System.Text.RegularExpressions;

namespace AiGuard;

/// <summary>
/// A single regex-based prompt-injection rule. Each match of <see cref="Pattern"/> produces one
/// <see cref="InjectionSignal"/> carrying the rule's identifier, description, weight and match span.
/// </summary>
public sealed class InjectionRule
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Creates a rule from a regex pattern. The pattern is compiled with
    /// <see cref="RegexOptions.IgnoreCase"/>, <see cref="RegexOptions.CultureInvariant"/> and
    /// <see cref="RegexOptions.Compiled"/>, and matching is bounded by a 250ms timeout.
    /// </summary>
    /// <param name="ruleId">Stable identifier of the rule, e.g. <c>INJ-001</c>. Must not be null, empty or whitespace.</param>
    /// <param name="description">Human readable explanation of what the rule detects.</param>
    /// <param name="weight">Contribution of each match to the overall score, in the range [0, 1].</param>
    /// <param name="pattern">Regex pattern source.</param>
    /// <exception cref="ArgumentException"><paramref name="ruleId"/> is null, empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight"/> is outside [0, 1] or NaN.</exception>
    public InjectionRule(string ruleId, string description, double weight, string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(pattern);

        // Written as a positive comparison so that NaN is rejected as well.
        if (!(weight >= 0d && weight <= 1d))
        {
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight must be in the range [0, 1].");
        }

        RuleId = ruleId;
        Description = description;
        Weight = weight;
        Pattern = new Regex(
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            MatchTimeout);
    }

    /// <summary>Stable identifier of the rule, e.g. <c>INJ-001</c>.</summary>
    public string RuleId { get; }

    /// <summary>Human readable explanation of what the rule detects.</summary>
    public string Description { get; }

    /// <summary>Contribution of each match to the overall score, in the range [0, 1].</summary>
    public double Weight { get; }

    /// <summary>Compiled regex used to find matches.</summary>
    public Regex Pattern { get; }

    /// <summary>
    /// Finds every match of <see cref="Pattern"/> in <paramref name="text"/> and yields one signal per match.
    /// </summary>
    /// <param name="text">The text to scan. Must not be <see langword="null"/>.</param>
    /// <returns>A lazily evaluated sequence of signals, empty when nothing matches.</returns>
    public IEnumerable<InjectionSignal> Match(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return MatchCore(text);
    }

    private IEnumerable<InjectionSignal> MatchCore(string text)
    {
        foreach (Match m in Pattern.Matches(text))
        {
            yield return new InjectionSignal(RuleId, Description, Weight, new TextSpan(m.Index, m.Length));
        }
    }
}
