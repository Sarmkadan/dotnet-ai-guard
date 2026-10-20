using System.Text.RegularExpressions;

namespace AiGuard;

/// <summary>
/// Detects prompt injection attempts using a set of regex-based rules.
/// Each matched rule contributes a weighted signal; the total score is clamped to [0, 1].
/// </summary>
public sealed class RuleBasedPromptInjectionDetector : IPromptInjectionDetector
{
    private static readonly List<(string RuleId, string Description, Regex Pattern, double Weight)> Rules = new()
    {
        ("INJ-001", "Instruction override attempt", new Regex(@"ignore\s+(all\s+)?previous\s+instructions", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.9),
        ("INJ-002", "Role hijacking", new Regex(@"you\s+are\s+now\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.8),
        ("INJ-003", "System prompt exfiltration", new Regex(@"(reveal|show|print|output)\s+(your\s+)?(system\s+prompt|instructions)", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.85),
        ("INJ-004", "Delimiter smuggling", new Regex(@"<\|?(system|im_start|im_end)\|?>", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.75),
        ("INJ-005", "DAN/jailbreak pattern", new Regex(@"(do\s+anything\s+now|DAN\s+mode|act\s+as\s+if\s+you\s+have\s+no\s+restrictions)", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.85),
        ("INJ-006", "Forget instructions", new Regex(@"forget\s+(all\s+)?(your\s+)?(previous\s+)?instructions", RegexOptions.IgnoreCase | RegexOptions.Compiled), 0.9),
    };

    private readonly double _threshold;

    /// <summary>
    /// Creates a new detector with the given injection threshold.
    /// </summary>
    /// <param name="threshold">Score at or above which input is considered an injection. Default 0.7.</param>
    public RuleBasedPromptInjectionDetector(double threshold = 0.7)
    {
        _threshold = threshold;
    }

    /// <inheritdoc />
    public ValueTask<InjectionVerdict> DetectAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var signals = new List<InjectionSignal>();
        double totalScore = 0;

        foreach (var (ruleId, description, pattern, weight) in Rules)
        {
            var match = pattern.Match(text);
            if (match.Success)
            {
                signals.Add(new InjectionSignal(ruleId, description, weight,
                    new TextSpan(match.Index, match.Length)));
                totalScore += weight;
            }
        }

        totalScore = Math.Min(totalScore, 1.0);
        var isInjection = totalScore >= _threshold;

        return ValueTask.FromResult(new InjectionVerdict(isInjection, totalScore, signals));
    }
}
