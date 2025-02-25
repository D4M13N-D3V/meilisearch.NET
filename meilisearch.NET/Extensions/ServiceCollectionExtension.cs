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
        return services;
    }
}