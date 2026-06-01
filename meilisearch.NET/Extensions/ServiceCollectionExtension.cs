using meilisearch.NET.Configurations;
using Microsoft.Extensions.DependencyInjection;
using meilisearch.NET;
namespace meilisearch.NET.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMeiliSearchService(this IServiceCollection services)
    {
        services.AddHttpClient<MeilisearchService>();
        services.AddSingleton<MeiliSearchConfiguration>();
        services.AddSingleton<MeilisearchService>();
        // Start/stop the same singleton via the host lifecycle (non-blocking startup).
        services.AddHostedService(sp => sp.GetRequiredService<MeilisearchService>());
        return services;
    }
}