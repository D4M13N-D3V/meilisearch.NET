using System.Text;

namespace meilisearch.NET;

public class ApiKeyGenerator
{
    private static readonly char[] Chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static string GenerateApiKey(int length = 64)
    {
        var random = new Random();
        var apiKey = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            apiKey.Append(Chars[random.Next(Chars.Length)]);
        }

        return apiKey.ToString();
    }
}