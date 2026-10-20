using System.Text.RegularExpressions;

namespace AiGuard;

/// <summary>
/// Redacts PII from text using regular expressions. Each category has its own pattern and
/// matched entities are replaced with numbered placeholders like <c>[EMAIL_1]</c>.
/// </summary>
public sealed class RegexPiiRedactor : IPiiRedactor
{
    private readonly PiiCategory _categories;

    private static readonly List<(PiiCategory Category, string Label, Regex Pattern)> Patterns = new()
    {
        (PiiCategory.Email, "EMAIL", new Regex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)),
        (PiiCategory.Phone, "PHONE", new Regex(@"(\+?\d{1,3}[\s\-]?)?\(?\d{2,4}\)?[\s\-]?\d{3,4}[\s\-]?\d{2,4}", RegexOptions.Compiled)),
        (PiiCategory.CreditCard, "CARD", new Regex(@"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", RegexOptions.Compiled)),
        (PiiCategory.Iban, "IBAN", new Regex(@"\b[A-Z]{2}\d{2}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{4}[\s]?[\dA-Z]{0,4}[\s]?[\dA-Z]{0,4}[\s]?[\dA-Z]{0,2}\b", RegexOptions.Compiled)),
        (PiiCategory.IpAddress, "IP", new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)),
    };

    /// <summary>
    /// Creates a new redactor that targets the specified PII categories.
    /// </summary>
    /// <param name="categories">Bitmask of categories to redact.</param>
    public RegexPiiRedactor(PiiCategory categories = PiiCategory.All)
    {
        _categories = categories;
    }

    /// <inheritdoc />
    public ValueTask<RedactionResult> RedactAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var entities = new List<PiiEntity>();
        var counters = new Dictionary<string, int>();
        var redacted = text;

        foreach (var (category, label, pattern) in Patterns)
        {
            if (!_categories.HasFlag(category))
            {
                continue;
            }

            // Collect matches on the current (possibly already partially redacted) text.
            // We re-match after each replacement to keep spans correct.
            Match match;
            while ((match = pattern.Match(redacted)).Success)
            {
                if (!counters.TryGetValue(label, out var count))
                {
                    count = 0;
                }
                count++;
                counters[label] = count;

                var placeholder = $"[{label}_{count}]";
                entities.Add(new PiiEntity(category, new TextSpan(match.Index, match.Length), placeholder));
                redacted = redacted.Remove(match.Index, match.Length).Insert(match.Index, placeholder);
            }
        }

        return ValueTask.FromResult(new RedactionResult(redacted, entities));
    }
}
