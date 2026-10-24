using System.Text.RegularExpressions;

namespace AiGuard;

/// <summary>
/// Restores original PII values in place of the placeholders produced by <see cref="IPiiRedactor"/>
/// (for example <c>[EMAIL_1]</c>), so that text returned by an LLM can be shown to the user with
/// the real values filled back in.
/// </summary>
public static class PlaceholderRehydrator
{
    private static readonly Regex _placeholderPattern = new(
        @"\[[A-Z]+_\d+\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Replaces every placeholder from <paramref name="result"/> that occurs in
    /// <paramref name="redactedOrLlmOutput"/> with the original substring it stood for.
    /// </summary>
    /// <param name="redactedOrLlmOutput">Text containing placeholders, typically an LLM response to a redacted prompt.</param>
    /// <param name="originalText">The text that was passed to the redactor; entity spans index into it.</param>
    /// <param name="result">The redaction result that produced the placeholders.</param>
    /// <returns>
    /// The input with known placeholders replaced. Placeholders that are not in <paramref name="result"/>
    /// and any other bracketed tokens are left untouched.
    /// </returns>
    public static string Rehydrate(string redactedOrLlmOutput, string originalText, RedactionResult result)
    {
        ArgumentNullException.ThrowIfNull(redactedOrLlmOutput);
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(result);

        var spans = new Dictionary<string, TextSpan>(StringComparer.Ordinal);
        foreach (var entity in result.Entities)
        {
            spans.TryAdd(entity.Placeholder, entity.Span);
        }

        return _placeholderPattern.Replace(redactedOrLlmOutput, match =>
        {
            if (!spans.TryGetValue(match.Value, out var span))
            {
                return match.Value;
            }

            return originalText.Substring(span.Start, span.Length);
        });
    }
}
