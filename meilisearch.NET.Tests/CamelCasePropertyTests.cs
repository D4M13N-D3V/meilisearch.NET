using meilisearch.NET;
using Xunit;

namespace meilisearch.NET.Tests;

public class CamelCasePropertyTests
{
    private class Sample
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }

    [Fact]
    public void GetPropertiesInCamelCase_CamelCasesPublicInstanceProperties()
    {
        var names = MeilisearchService.GetPropertiesInCamelCase<Sample>();

        Assert.Contains("id", names);
        Assert.Contains("firstName", names);
        // Already-lowercase names are left unchanged.
        Assert.Contains("message", names);
        Assert.Equal(3, names.Length);
    }
}
