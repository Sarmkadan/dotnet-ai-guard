using System.Numerics;
using System.Text.RegularExpressions;

namespace AiGuard;

/// <summary>
/// A single PII recognizer: a regular expression for one <see cref="PiiCategory"/> plus an optional
/// post-match validator (for example a Luhn checksum for payment card numbers).
/// </summary>
public sealed class PiiPattern
{
    /// <summary>Upper bound on how long a single regex match attempt may run, guarding against ReDoS.</summary>
    private static readonly TimeSpan _matchTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Creates a new recognizer for <paramref name="category"/>.
    /// </summary>
    /// <param name="category">The single PII category this pattern detects. Must have exactly one flag set.</param>
    /// <param name="pattern">Regular expression source. The compiled regex uses <see cref="RegexOptions.CultureInvariant"/>,
    /// <see cref="RegexOptions.Compiled"/> and a 250 ms match timeout.</param>
    /// <param name="validator">Optional predicate applied to each regex match value; matches for which it returns
    /// <see langword="false"/> are discarded.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not exactly one flag.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    public PiiPattern(PiiCategory category, string pattern, Func<string, bool>? validator = null)
    {
        if (BitOperations.PopCount((uint)category) != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(category), category,
                "PII pattern must target exactly one PiiCategory flag.");
        }

        ArgumentNullException.ThrowIfNull(pattern);

        Category = category;
        Pattern = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled, _matchTimeout);
        Validator = validator;
    }

    /// <summary>Category assigned to every match produced by this pattern.</summary>
    public PiiCategory Category { get; }

    /// <summary>Compiled regular expression used to find candidate matches.</summary>
    public Regex Pattern { get; }

    /// <summary>Optional post-match validation; <see langword="null"/> accepts every regex match.</summary>
    public Func<string, bool>? Validator { get; }

    /// <summary>
    /// Finds all matches of <see cref="Pattern"/> in <paramref name="text"/> that pass <see cref="Validator"/>.
    /// </summary>
    /// <param name="text">Text to scan. Must not be <see langword="null"/>.</param>
    /// <returns>Span and category for each accepted match, in document order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="RegexMatchTimeoutException">A match attempt exceeded the 250 ms timeout.</exception>
    public IEnumerable<(TextSpan Span, PiiCategory Category)> Match(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MatchCore(text);
    }

    private IEnumerable<(TextSpan Span, PiiCategory Category)> MatchCore(string text)
    {
        for (var match = Pattern.Match(text); match.Success; match = match.NextMatch())
        {
            if (Validator is null || Validator(match.Value))
            {
                yield return (new TextSpan(match.Index, match.Length), Category);
            }
        }
    }
}
