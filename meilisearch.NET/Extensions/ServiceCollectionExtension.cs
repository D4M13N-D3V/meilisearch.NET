using meilisearch.NET.Configurations;
using meilisearch.NET.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace meilisearch.NET.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMeiliSearchService(this IServiceCollection services)
    {
        services.AddHttpClient<MeiliSearchService>();
        services.AddSingleton<MeiliSearchConfiguration>();
        services.AddSingleton<IMeiliSearchService, MeiliSearchService>();
        return services;
    }
}