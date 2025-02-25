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
        // Set security protocol to SystemDefault
        ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

        // Configure and build the host
        IHost host = CreateHostBuilder(args).Build();

        //Resolve test dependency
        var testService = host.Services.GetService<test>();

        // Run the host
        await host.RunAsync();
        
    }

    
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                configuration.AddEnvironmentVariables(); // Add environment variables as well (optional, but good practice)
                configuration.AddCommandLine(args); // Support command line arguments
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
                options.SuppressStatusMessages = true;  // This is optional: you can suppress the "Application started" message
            });
}

public class test
{
    private readonly ILogger<test> _logger;

    public test(MeilisearchService service, ILogger<test> logger)
    {
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // You can perform actions with the Meilisearch service here
        _logger.LogInformation("Test service initialized.");
    }
}