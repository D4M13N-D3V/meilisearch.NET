using System.Net;
using System.Threading;
using System.Threading.Tasks;
using meilisearch.NET;
using meilisearch.NET.example;
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
                // Registered after the Meilisearch service so it starts only
                // once the server's StartAsync has completed.
                services.AddHostedService<TestWorker>();

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

public class TestWorker : IHostedService
{
    private readonly MeilisearchService _service;
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(MeilisearchService service, ILogger<TestWorker> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _service.CreateIndexAsync<document>("test");

        for (var i = 0; i < 7; i++)
        {
            _service.AddDocument("test", new document
            {
                Id = Guid.NewGuid(),
                message = "Hello, Meilisearch!"
            });
        }

        _logger.LogInformation("Test worker initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
