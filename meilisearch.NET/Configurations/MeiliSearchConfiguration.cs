using Microsoft.Extensions.Configuration;

namespace meilisearch.NET.Configurations;

public class MeiliSearchConfiguration
{
    private readonly string _meiliPort = "Meili:Port";
    private readonly string _meiliUiEnabled = "Meili:UiEnabled";
    private readonly IConfiguration _configuration;
    public MeiliSearchConfiguration(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    public int MeiliPort => _configuration.GetValue<int>(_meiliPort);
    public bool UiEnabled => _configuration.GetValue<bool>(_meiliUiEnabled);
}