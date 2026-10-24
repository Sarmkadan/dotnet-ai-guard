using System.Text;
using Microsoft.Extensions.Options;

namespace AiGuard;

/// <summary>
/// Redacts PII from text using <see cref="PiiPattern"/> recognizers. Only categories enabled in
/// <see cref="AiGuardOptions.RedactCategories"/> are applied; the setting is read on every call so
/// it can change at runtime. Overlapping matches are resolved in favour of the earlier (and then longer)
/// span, and each entity is replaced with a numbered placeholder such as <c>[EMAIL_1]</c>.
/// </summary>
public sealed class RegexPiiRedactor : IPiiRedactor
{
    private readonly IOptionsMonitor<AiGuardOptions> _options;
    private readonly IReadOnlyList<PiiPattern> _patterns;

    /// <summary>
    /// Creates a new redactor.
    /// </summary>
    /// <param name="options">Source of <see cref="AiGuardOptions.RedactCategories"/>.</param>
    /// <param name="extraPatterns">Additional recognizers run after <see cref="DefaultPiiPatterns.All"/>. Optional.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public RegexPiiRedactor(IOptionsMonitor<AiGuardOptions> options, IEnumerable<PiiPattern>? extraPatterns = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _patterns = extraPatterns is null
            ? DefaultPiiPatterns.All
            : [.. DefaultPiiPatterns.All, .. extraPatterns];
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>. Thrown synchronously, not via the returned task.</exception>
    public ValueTask<RedactionResult> RedactAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        var enabled = _options.CurrentValue.RedactCategories;
        var found = new List<(TextSpan Span, PiiCategory Category)>();

        foreach (var pattern in _patterns)
        {
            if ((enabled & pattern.Category) == 0)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            found.AddRange(pattern.Match(text));
        }

        if (found.Count == 0)
        {
            return ValueTask.FromResult(new RedactionResult(text, Array.Empty<PiiEntity>()));
        }

        // Earlier start wins; for equal starts the longer span wins. Anything starting before the
        // end of the last kept span overlaps it and is dropped.
        found.Sort(static (a, b) =>
        {
            var byStart = a.Span.Start.CompareTo(b.Span.Start);
            return byStart != 0 ? byStart : b.Span.Length.CompareTo(a.Span.Length);
        });

        var counters = new Dictionary<PiiCategory, int>();
        var entities = new List<PiiEntity>(found.Count);
        var builder = new StringBuilder(text.Length);
        var cursor = 0;

        foreach (var (span, category) in found)
        {
            if (span.Start < cursor)
            {
                continue;
            }

            counters.TryGetValue(category, out var count);
            count++;
            counters[category] = count;

            var placeholder = $"[{category.ToString().ToUpperInvariant()}_{count}]";
            entities.Add(new PiiEntity(category, span, placeholder));

            builder.Append(text, cursor, span.Start - cursor);
            builder.Append(placeholder);
            cursor = span.Start + span.Length;
        }

        builder.Append(text, cursor, text.Length - cursor);

        return ValueTask.FromResult(new RedactionResult(builder.ToString(), entities));
    }
}
