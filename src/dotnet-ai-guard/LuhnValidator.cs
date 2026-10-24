namespace AiGuard;

/// <summary>
/// Validates digit sequences (such as payment card numbers) with the Luhn mod-10 checksum.
/// </summary>
public static class LuhnValidator
{
    private const int MinDigits = 13;
    private const int MaxDigits = 19;

    /// <summary>
    /// Determines whether <paramref name="digitsWithSeparators"/> is a Luhn-valid number of 13 to 19 digits.
    /// Spaces and dashes are ignored; any other character makes the result <see langword="false"/>.
    /// </summary>
    /// <param name="digitsWithSeparators">Candidate number, e.g. <c>4539 1488-0343 6467</c>.</param>
    /// <returns>
    /// <see langword="true"/> when the candidate contains 13 to 19 digits and passes the Luhn checksum;
    /// otherwise <see langword="false"/>. Never throws.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> digitsWithSeparators)
    {
        int count = 0;
        int sum = 0;
        bool doubleDigit = false;

        // Walk right to left so every second digit (starting with the one before the check digit) is doubled.
        for (int i = digitsWithSeparators.Length - 1; i >= 0; i--)
        {
            char c = digitsWithSeparators[i];

            if (c == ' ' || c == '-')
            {
                continue;
            }

            if (c < '0' || c > '9')
            {
                return false;
            }

            if (++count > MaxDigits)
            {
                return false;
            }

            int digit = c - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return count >= MinDigits && sum % 10 == 0;
    }
}
