using System.Security.Cryptography;
using System.Text;

namespace meilisearch.NET;

/// <summary>
/// Generates Meilisearch master keys using a cryptographically secure RNG.
/// This is the single canonical key generator for the library.
/// </summary>
public static class ApiKeyGenerator
{
    // 64 characters: charset length divides 256 evenly, so the byte-to-char
    // mapping below is free of modulo bias.
    private const string AllowedChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static string GenerateApiKey(int length = 64)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than zero.", nameof(length));
        }

        var apiKey = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            // GetInt32 uses a CSPRNG and rejection-samples internally, so the
            // result is uniform regardless of the charset length.
            apiKey.Append(AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)]);
        }

        return apiKey.ToString();
    }
}
