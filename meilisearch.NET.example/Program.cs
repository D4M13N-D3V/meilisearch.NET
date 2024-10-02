using System.Net;
using meilisearch.NET;
using meilisearch.NET.Configurations;
using meilisearch.NET.Extensions;
using meilisearch.NET.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddMeiliSearchService();
builder.Services.AddSingleton<test>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Services.AddLogging();
var app = builder.Build();
app.Run();
Console.ReadLine();

public class test
{
    public test(IMeiliSearchService service)
    {
        service.Start();
    }
}