using meilisearch.NET;
using Xunit;

namespace meilisearch.NET.Tests;

public class ApiKeyGeneratorTests
{
    [Fact]
    public void GenerateApiKey_DefaultLength_Is64()
    {
        Assert.Equal(64, ApiKeyGenerator.GenerateApiKey().Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(128)]
    public void GenerateApiKey_RespectsRequestedLength(int length)
    {
        Assert.Equal(length, ApiKeyGenerator.GenerateApiKey(length).Length);
    }

    [Fact]
    public void GenerateApiKey_UsesOnlyAllowedCharacters()
    {
        const string allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var key = ApiKeyGenerator.GenerateApiKey(512);

        Assert.All(key, c => Assert.Contains(c, allowed));
    }

    [Fact]
    public void GenerateApiKey_ProducesDistinctKeys()
    {
        // Cryptographically random keys should effectively never collide.
        var keys = Enumerable.Range(0, 50)
            .Select(_ => ApiKeyGenerator.GenerateApiKey())
            .ToHashSet();

        Assert.Equal(50, keys.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GenerateApiKey_NonPositiveLength_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => ApiKeyGenerator.GenerateApiKey(length));
    }
}
