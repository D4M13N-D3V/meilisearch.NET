using Microsoft.Extensions.Configuration;

namespace meilisearch.NET.Configurations;

public class MeiliSearchConfiguration
{
    private readonly string _meiliPort = "Meili:Port";
    private readonly string _meiliUiEnabled = "Meili:UiEnabled";
    private readonly string _meiliEnableCustomApiKey = "Meili:CustomApiKey";
    private readonly string _meiliApiKey = "Meili:ApiKey";
    private readonly IConfiguration _configuration;
    public MeiliSearchConfiguration(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    public int MeiliPort => _configuration.GetValue<int>(_meiliPort);
    public bool UiEnabled => _configuration.GetValue<bool>(_meiliUiEnabled);
    public string ApiKey => _configuration.GetValue<string>(_meiliApiKey) ?? "";
    public bool EnableCustomApiKey => _configuration.GetValue<bool>(_meiliEnableCustomApiKey);
}