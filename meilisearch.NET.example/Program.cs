using System;
using System.Net;
using System.Threading.Tasks;
using meilisearch.NET;
using meilisearch.NET.Configurations;
using meilisearch.NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
        IHost host = CreateHostBuilder(args).Build();
        var testService = host.Services.GetService<test>();
        await host.RunAsync();
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                configuration.AddEnvironmentVariables(); 
                configuration.AddCommandLine(args); 
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMeiliSearchService();
                services.AddSingleton<test>();

                // Add logging configuration
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
             })
            .UseConsoleLifetime(options =>
            {
                options.SuppressStatusMessages = true; 
            });
}

public class test
{
    private readonly ILogger<test> _logger;

    public test(MeilisearchService service, ILogger<test> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Wait until Meilisearch is running
        while (!service.IsMeilisearchRunning())
        {
            _logger.LogInformation("Waiting for Meilisearch to start...");
            Task.Delay(1000).Wait(); // Wait for 1 second before checking again
        }

        service.CreateIndex("test");
        _logger.LogInformation("Test service initialized.");
    }
}
