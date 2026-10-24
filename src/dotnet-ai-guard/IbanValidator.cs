namespace AiGuard;

/// <summary>
/// Validates International Bank Account Numbers (IBAN) with the ISO 13616 mod-97 check.
/// </summary>
public static class IbanValidator
{
    private const int MinLength = 15;
    private const int MaxLength = 34;
    private const int Modulus = 97;

    /// <summary>
    /// Expected total IBAN lengths per country code (SWIFT IBAN registry).
    /// Country codes that are not listed are checked against the generic length range only.
    /// </summary>
    private static readonly Dictionary<string, int> ExpectedLengths = new(StringComparer.Ordinal)
    {
        ["AT"] = 20,
        ["BE"] = 16,
        ["BG"] = 22,
        ["CH"] = 21,
        ["CZ"] = 24,
        ["DE"] = 22,
        ["DK"] = 18,
        ["EE"] = 20,
        ["ES"] = 24,
        ["FI"] = 18,
        ["FR"] = 27,
        ["GB"] = 22,
        ["GR"] = 27,
        ["HR"] = 21,
        ["HU"] = 28,
        ["IE"] = 22,
        ["IS"] = 26,
        ["IT"] = 27,
        ["LI"] = 21,
        ["LT"] = 20,
        ["LU"] = 20,
        ["LV"] = 21,
        ["MT"] = 31,
        ["NL"] = 18,
        ["NO"] = 15,
        ["PL"] = 28,
        ["PT"] = 25,
        ["RO"] = 24,
        ["SE"] = 24,
        ["SI"] = 19,
        ["SK"] = 24,
        ["UA"] = 29,
    };

    private static readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> ExpectedLengthsLookup =
        ExpectedLengths.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>
    /// Checks whether <paramref name="iban"/> is a structurally valid IBAN.
    /// </summary>
    /// <remarks>
    /// Spaces are ignored and letters are case-insensitive. The value must be 15-34 ASCII
    /// letters and digits, start with a two-letter country code followed by two check digits,
    /// and, for known country codes, have the length registered for that country.
    /// The check does not verify that the bank or account exists.
    /// </remarks>
    /// <param name="iban">Candidate IBAN, optionally written in blocks separated by spaces.</param>
    /// <returns><see langword="true"/> when the IBAN passes the format and mod-97 checks; otherwise <see langword="false"/>.</returns>
    public static bool IsValid(ReadOnlySpan<char> iban)
    {
        Span<char> normalized = stackalloc char[MaxLength];
        int length = 0;

        foreach (char raw in iban)
        {
            if (raw == ' ')
            {
                continue;
            }

            char c = ToUpperAscii(raw);
            if (!IsAsciiUpperLetter(c) && !IsAsciiDigit(c))
            {
                return false;
            }

            if (length == MaxLength)
            {
                return false;
            }

            normalized[length++] = c;
        }

        if (length < MinLength ||
            !IsAsciiUpperLetter(normalized[0]) ||
            !IsAsciiUpperLetter(normalized[1]) ||
            !IsAsciiDigit(normalized[2]) ||
            !IsAsciiDigit(normalized[3]))
        {
            return false;
        }

        if (ExpectedLengthsLookup.TryGetValue(normalized[..2], out int expectedLength) && length != expectedLength)
        {
            return false;
        }

        return ComputeMod97(normalized[..length]) == 1;
    }

    /// <summary>
    /// Computes the mod-97 remainder of the IBAN after moving the first four characters to the end
    /// and replacing letters with two-digit numbers (A=10 ... Z=35). Processes one character at a time,
    /// so the number never needs to be materialized.
    /// </summary>
    private static int ComputeMod97(ReadOnlySpan<char> iban)
    {
        int length = iban.Length;
        int remainder = 0;

        for (int i = 0; i < length; i++)
        {
            char c = iban[(i + 4) % length];

            if (IsAsciiDigit(c))
            {
                remainder = (remainder * 10 + (c - '0')) % Modulus;
            }
            else
            {
                int value = c - 'A' + 10;
                remainder = (remainder * 100 + value) % Modulus;
            }
        }

        return remainder;
    }

    private static char ToUpperAscii(char c) => c is >= 'a' and <= 'z' ? (char)(c - 32) : c;

    private static bool IsAsciiUpperLetter(char c) => c is >= 'A' and <= 'Z';

    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';
}
