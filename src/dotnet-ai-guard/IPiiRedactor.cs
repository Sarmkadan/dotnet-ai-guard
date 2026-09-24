namespace AiGuard;

/// <summary>
/// Finds and masks personally identifiable information (emails, phone numbers,
/// IBANs, credit cards, national IDs, IP addresses, ...) in text before it is sent to an LLM
/// or written to logs.
/// </summary>
public interface IPiiRedactor
{
    /// <summary>
    /// Redacts PII from <paramref name="text"/> according to the configured <see cref="PiiCategory"/> set.
    /// </summary>
    /// <param name="text">Input text. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The redacted text together with the list of detected entities.</returns>
    ValueTask<RedactionResult> RedactAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a redaction pass.
/// </summary>
/// <param name="RedactedText">Text with every detected entity replaced by a placeholder.</param>
/// <param name="Entities">Entities that were found, in document order.</param>
public sealed record RedactionResult(string RedactedText, IReadOnlyList<PiiEntity> Entities)
{
    /// <summary><see langword="true"/> when at least one entity was redacted.</summary>
    public bool HasRedactions => Entities.Count > 0;
}

/// <summary>
/// A detected PII entity.
/// </summary>
/// <param name="Category">Kind of PII.</param>
/// <param name="Span">Location within the original text.</param>
/// <param name="Placeholder">Token that replaced the entity, e.g. <c>[EMAIL_1]</c>.</param>
public sealed record PiiEntity(PiiCategory Category, TextSpan Span, string Placeholder);

/// <summary>
/// Categories of personally identifiable information the redactor can detect.
/// </summary>
[Flags]
public enum PiiCategory
{
    /// <summary>No category.</summary>
    None = 0,
    /// <summary>Email addresses.</summary>
    Email = 1 << 0,
    /// <summary>Phone numbers in international and local formats.</summary>
    Phone = 1 << 1,
    /// <summary>Payment card numbers (Luhn-validated).</summary>
    CreditCard = 1 << 2,
    /// <summary>IBAN bank account numbers.</summary>
    Iban = 1 << 3,
    /// <summary>IPv4 and IPv6 addresses.</summary>
    IpAddress = 1 << 4,
    /// <summary>National identifiers (SSN, tax IDs, passport numbers).</summary>
    NationalId = 1 << 5,
    /// <summary>Person names detected via heuristics or a pluggable NER provider.</summary>
    PersonName = 1 << 6,
    /// <summary>Every supported category.</summary>
    All = Email | Phone | CreditCard | Iban | IpAddress | NationalId | PersonName,
}
