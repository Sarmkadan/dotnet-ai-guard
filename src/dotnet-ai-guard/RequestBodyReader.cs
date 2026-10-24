using System.Text;
using Microsoft.AspNetCore.Http;

namespace AiGuard;

/// <summary>
/// Buffers request bodies for <see cref="AiGuardMiddleware"/> and swaps in redacted payloads.
/// </summary>
internal static class RequestBodyReader
{
    private const int _chunkSize = 4096;

    /// <summary>
    /// Enables buffering on <paramref name="request"/> and reads its body as UTF-8 text.
    /// The body stream is rewound to the start before returning, so downstream middleware can read it again.
    /// </summary>
    /// <param name="request">The request whose body is read.</param>
    /// <param name="maxBodyBytes">Upper bound for the body, in characters read from the stream.</param>
    /// <param name="ct">Token used to cancel the read.</param>
    /// <returns>The body text, or <c>null</c> when the declared or actual body exceeds <paramref name="maxBodyBytes"/>.</returns>
    public static async Task<string?> ReadAsync(HttpRequest request, long maxBodyBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.EnableBuffering();

        if (request.ContentLength > maxBodyBytes)
        {
            return null;
        }

        var limit = maxBodyBytes + 1;
        var builder = new StringBuilder();
        var buffer = new char[(int)Math.Min(_chunkSize, limit)];

        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);

            while (builder.Length < limit)
            {
                var room = (int)Math.Min(buffer.Length, limit - builder.Length);
                var read = await reader.ReadAsync(buffer.AsMemory(0, room), ct);
                if (read == 0)
                {
                    break;
                }

                builder.Append(buffer, 0, read);
            }

            return builder.Length > maxBodyBytes ? null : builder.ToString();
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    /// <summary>
    /// Replaces the request body with the UTF-8 encoding of <paramref name="newBody"/> and updates <c>Content-Length</c>.
    /// </summary>
    /// <param name="request">The request whose body is replaced.</param>
    /// <param name="newBody">The new body text.</param>
    public static void ReplaceBody(HttpRequest request, string newBody)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(newBody);

        var bytes = Encoding.UTF8.GetBytes(newBody);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
    }
}
