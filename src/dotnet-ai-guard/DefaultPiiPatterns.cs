using System.Net;
using System.Net.Sockets;

namespace AiGuard;

/// <summary>
/// Built-in <see cref="PiiPattern"/> recognizers covering every concrete <see cref="PiiCategory"/>.
/// Candidate matches are kept deliberately bounded (no unbounded nested quantifiers) so the
/// regexes stay linear; checksum or range validators then discard false positives.
/// </summary>
public static class DefaultPiiPatterns
{
    /// <summary>Weights for the Bulgarian EGN check digit.</summary>
    private static readonly int[] EgnWeights = [2, 4, 8, 5, 10, 9, 7, 3, 6];

    /// <summary>
    /// All built-in PII patterns, grouped by category in the order of <see cref="PiiCategory"/>.
    /// </summary>
    public static IReadOnlyList<PiiPattern> All { get; } =
    [
        // Email: local part, dotted domain labels, alphabetic TLD. Quantifiers are bounded.
        new(PiiCategory.Email, @"(?<![\w.%+\-])[A-Za-z0-9._%+\-]{1,64}@(?:[A-Za-z0-9\-]{1,63}\.){1,8}[A-Za-z]{2,24}(?![\w\-])"),

        // Phone: optional +CC, optional (area) group, digits separated by spaces, dashes or parentheses.
        // Dots are deliberately not separators so dotted IPv4 addresses are not read as phone numbers.
        // The digit count (7-15, E.164 upper bound) is checked by the validator.
        new(PiiCategory.Phone, @"(?<![\w+])\+?\(?[0-9][0-9 ()\-]{5,24}[0-9](?!\w)", IsValidPhone),

        // Payment card: 13-19 digits with optional single space or dash between digits. Luhn decides.
        new(PiiCategory.CreditCard, @"(?<![0-9])[0-9](?:[ \-]?[0-9]){12,18}(?![0-9])", s => LuhnValidator.IsValid(s)),

        // IBAN: country code, check digits, then 11-30 alphanumerics, optionally space-separated.
        new(PiiCategory.Iban, @"(?<![A-Za-z0-9])[A-Za-z]{2}[0-9]{2}(?: ?[A-Za-z0-9]){11,30}(?![A-Za-z0-9])", s => IbanValidator.IsValid(s)),

        // IPv4: dotted quad; octet range is checked by IPAddress.TryParse in the validator.
        new(PiiCategory.IpAddress, @"(?<![0-9A-Za-z.:])(?:[0-9]{1,3}\.){3}[0-9]{1,3}(?![0-9]|\.[0-9])", IsValidIpv4),

        // IPv6: full, compressed (::) and IPv4-suffixed forms. IPAddress.TryParse validates the shape.
        new(PiiCategory.IpAddress, @"(?<![0-9A-Za-z:.])(?:[0-9A-Fa-f]{0,4}:){2,7}(?:[0-9A-Fa-f]{0,4}|(?:[0-9]{1,3}\.){3}[0-9]{1,3})(?![0-9A-Za-z:]|\.[0-9A-Za-z:])", IsValidIpv6),

        // US Social Security Number, ddd-dd-dddd.
        new(PiiCategory.NationalId, @"(?<![0-9\-])[0-9]{3}-[0-9]{2}-[0-9]{4}(?![0-9\-])"),

        // German Steuer-ID: 11 digits, first digit non-zero, ISO 7064 (11,10) check digit.
        new(PiiCategory.NationalId, @"(?<![0-9])[1-9][0-9]{10}(?![0-9])", IsValidGermanTaxId),

        // Bulgarian EGN: 10 digits, YYMMDDNNNC with weighted check digit and encoded month/day.
        new(PiiCategory.NationalId, @"(?<![0-9])[0-9]{10}(?![0-9])", IsValidBulgarianEgn),
    ];

    private static bool IsValidPhone(string value)
    {
        int digits = 0;
        foreach (char c in value)
        {
            if (c is >= '0' and <= '9')
            {
                digits++;
            }
        }

        return digits is >= 7 and <= 15;
    }

    private static bool IsValidIpv4(string value) =>
        IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetwork;

    private static bool IsValidIpv6(string value) =>
        // Require at least one hex digit so a bare "::" is not reported as an address.
        value.Any(char.IsAsciiHexDigit) &&
        IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6;

    private static bool IsValidGermanTaxId(string value)
    {
        int product = 10;
        for (int i = 0; i < 10; i++)
        {
            int sum = (value[i] - '0' + product) % 10;
            if (sum == 0)
            {
                sum = 10;
            }

            product = sum * 2 % 11;
        }

        int check = 11 - product;
        if (check == 10)
        {
            check = 0;
        }

        return check == value[10] - '0';
    }

    private static bool IsValidBulgarianEgn(string value)
    {
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (value[i] - '0') * EgnWeights[i];
        }

        int check = sum % 11;
        if (check == 10)
        {
            check = 0;
        }

        if (check != value[9] - '0')
        {
            return false;
        }

        // Month is offset by 20 for 1800-1899 and by 40 for 2000-2099.
        int month = (value[2] - '0') * 10 + (value[3] - '0');
        if (month >= 40)
        {
            month -= 40;
        }
        else if (month >= 20)
        {
            month -= 20;
        }

        int day = (value[4] - '0') * 10 + (value[5] - '0');

        return month is >= 1 and <= 12 && day is >= 1 and <= 31;
    }
}
