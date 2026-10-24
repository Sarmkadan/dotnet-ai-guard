using System.Text;

namespace AiGuard;

/// <summary>
/// Undoes common obfuscation before prompt-injection rules run, so that a payload written with
/// compatibility forms, zero-width characters, Cyrillic look-alikes or irregular whitespace still
/// matches a rule written against plain ASCII.
/// </summary>
/// <remarks>
/// Steps, in order: Unicode NFKC normalization (which also folds fullwidth Latin letters to ASCII),
/// removal of zero-width characters (U+200B-U+200D, U+FEFF), replacement of Cyrillic homoglyphs with
/// their Latin equivalents, and collapsing of every run of whitespace into a single space.
/// The output is generally not the same length as the input.
/// </remarks>
public static class InjectionTextNormalizer
{
    /// <summary>
    /// Returns a normalized copy of <paramref name="text"/> for rule matching.
    /// </summary>
    /// <param name="text">The text to normalize. Must not be <see langword="null"/>.</param>
    /// <returns>The normalized text. Match spans produced against this string refer to these positions, not to the original.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> contains malformed UTF-16 (for example an unpaired surrogate).</exception>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var composed = text.Normalize(NormalizationForm.FormKC);
        var sb = new StringBuilder(composed.Length);
        var previousWasSpace = false;

        foreach (var c in composed)
        {
            if (IsZeroWidth(c))
            {
                continue;
            }

            var mapped = MapHomoglyph(c);

            if (char.IsWhiteSpace(mapped))
            {
                if (!previousWasSpace)
                {
                    sb.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            sb.Append(mapped);
            previousWasSpace = false;
        }

        return sb.ToString();
    }

    private static bool IsZeroWidth(char c) =>
        c is >= '​' and <= '‍' or '﻿';

    private static char MapHomoglyph(char c) => c switch
    {
        // Cyrillic lowercase
        'а' => 'a', // а
        'е' => 'e', // е
        'о' => 'o', // о
        'р' => 'p', // р
        'с' => 'c', // с
        'х' => 'x', // х

        // Cyrillic uppercase
        'А' => 'A', // А
        'Е' => 'E', // Е
        'О' => 'O', // О
        'Р' => 'P', // Р
        'С' => 'C', // С
        'Х' => 'X', // Х

        _ => c,
    };
}
