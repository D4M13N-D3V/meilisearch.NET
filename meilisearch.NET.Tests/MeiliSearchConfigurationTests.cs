using meilisearch.NET.Configurations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace meilisearch.NET.Tests;

public class MeiliSearchConfigurationTests
{
    private static MeiliSearchConfiguration Build(Dictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new MeiliSearchConfiguration(configuration);
    }

    [Fact]
    public void ReadsValuesFromMeiliSection()
    {
        var config = Build(new Dictionary<string, string?>
        {
            ["Meili:Port"] = "7701",
            ["Meili:UiEnabled"] = "true",
            ["Meili:CustomApiKey"] = "true",
            ["Meili:ApiKey"] = "my-key",
        });

        Assert.Equal(7701, config.MeiliPort);
        Assert.True(config.UiEnabled);
        Assert.True(config.EnableCustomApiKey);
        Assert.Equal("my-key", config.ApiKey);
    }

    [Fact]
    public void MissingApiKey_DefaultsToEmptyString()
    {
        var config = Build(new Dictionary<string, string?>
        {
            ["Meili:Port"] = "7700",
        });

        Assert.Equal(string.Empty, config.ApiKey);
        Assert.False(config.EnableCustomApiKey);
    }

    [Fact]
    public void NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MeiliSearchConfiguration(null!));
    }
}
