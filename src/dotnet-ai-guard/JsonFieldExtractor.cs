using System.Text.Json;
using System.Text.Json.Nodes;

namespace AiGuard;

/// <summary>
/// Reads and rewrites string values inside a JSON document addressed by dot-separated paths.
/// A path is a sequence of property names, and <c>[*]</c> after a name iterates every element of
/// that array, for example <c>messages[*].content</c> or <c>prompt</c>.
/// </summary>
internal static class JsonFieldExtractor
{
    private const string Wildcard = "[*]";

    /// <summary>
    /// Returns every string value that the <paramref name="paths"/> select from <paramref name="json"/>, in document order
    /// for each path. Non-string values are skipped. Returns an empty list when <paramref name="json"/> is
    /// not valid JSON.
    /// </summary>
    /// <param name="json">The JSON document to read.</param>
    /// <param name="paths">Dot-separated paths; each segment may end with one or more <c>[*]</c> wildcards.</param>
    /// <exception cref="ArgumentException">A path is malformed.</exception>
    public static IReadOnlyList<string> Extract(string json, IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(paths);

        var parsedPaths = ParsePaths(paths);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var site in CollectSites(root, parsedPaths))
        {
            values.Add(site.Value);
        }

        return values;
    }

    /// <summary>
    /// Replaces every string value that the <paramref name="paths"/> select with <paramref name="transform"/> applied to it,
    /// and returns the document re-serialized with <c>JsonNode.ToJsonString</c>.
    /// </summary>
    /// <param name="json">The JSON document to rewrite.</param>
    /// <param name="paths">Dot-separated paths; each segment may end with one or more <c>[*]</c> wildcards.</param>
    /// <param name="transform">Maps each matched string to its replacement.</param>
    /// <exception cref="ArgumentException">A path is malformed.</exception>
    /// <exception cref="JsonException">
    /// <paramref name="json"/> is not valid JSON. Unlike <see cref="Extract"/>, this does not return the input
    /// unchanged, so that a caller never forwards unredacted content by mistake.
    /// </exception>
    public static string Rewrite(string json, IEnumerable<string> paths, Func<string, string> transform)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(transform);

        var parsedPaths = ParsePaths(paths);

        var root = JsonNode.Parse(json);
        if (root is null)
        {
            return "null";
        }

        // Collect first, then replace, so the tree is not mutated while it is being walked.
        foreach (var site in CollectSites(root, parsedPaths))
        {
            var replacement = transform(site.Value);
            if (site.Key is not null)
            {
                ((JsonObject)site.Parent)[site.Key] = replacement;
            }
            else
            {
                ((JsonArray)site.Parent)[site.Index] = replacement;
            }
        }

        return root.ToJsonString();
    }

    private static List<Site> CollectSites(JsonNode? root, List<List<PathToken>> paths)
    {
        var sites = new List<Site>();
        foreach (var tokens in paths)
        {
            Walk(parent: null, key: null, index: -1, root, tokens, position: 0, sites);
        }

        return sites;
    }

    private static void Walk(
        JsonNode? parent,
        string? key,
        int index,
        JsonNode? node,
        List<PathToken> tokens,
        int position,
        List<Site> sites)
    {
        if (position == tokens.Count)
        {
            if (parent is not null && node is JsonValue value && value.GetValueKind() == JsonValueKind.String)
            {
                sites.Add(new Site(parent, key, index, value.GetValue<string>()));
            }

            return;
        }

        var token = tokens[position];
        if (token.Property is not null)
        {
            if (node is JsonObject obj && obj.TryGetPropertyValue(token.Property, out var child))
            {
                Walk(obj, token.Property, -1, child, tokens, position + 1, sites);
            }
        }
        else if (node is JsonArray array)
        {
            for (var i = 0; i < array.Count; i++)
            {
                Walk(array, key: null, i, array[i], tokens, position + 1, sites);
            }
        }
    }

    private static List<List<PathToken>> ParsePaths(IEnumerable<string> paths)
    {
        var parsed = new List<List<PathToken>>();
        foreach (var path in paths)
        {
            parsed.Add(ParsePath(path));
        }

        return parsed;
    }

    private static List<PathToken> ParsePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var tokens = new List<PathToken>();
        foreach (var segment in path.Split('.'))
        {
            var bracket = segment.IndexOf('[', StringComparison.Ordinal);
            var name = bracket < 0 ? segment : segment[..bracket];
            if (name.Length == 0)
            {
                throw new ArgumentException($"Path '{path}' contains an empty property name.", nameof(path));
            }

            tokens.Add(new PathToken(name));

            var rest = bracket < 0 ? string.Empty : segment[bracket..];
            while (rest.Length > 0)
            {
                if (!rest.StartsWith(Wildcard, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Path '{path}' has an invalid segment '{segment}'; only [*] is supported.", nameof(path));
                }

                tokens.Add(new PathToken(null));
                rest = rest[Wildcard.Length..];
            }
        }

        return tokens;
    }

    /// <summary>One step of a parsed path: a property name, or a wildcard when <see cref="Property"/> is null.</summary>
    private readonly record struct PathToken(string? Property);

    /// <summary>A matched string value together with the slot that holds it, so it can be replaced.</summary>
    private readonly record struct Site(JsonNode Parent, string? Key, int Index, string Value);
}
