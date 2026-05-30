# MeiliSearch .NET Embedded

![GitHub Release](https://img.shields.io/github/v/release/D4M13N-D3V/meilisearch.NET)
![Dotnet 8](https://img.shields.io/badge/-.NET%208.0-blueviolet?logo=dotnet)
![Meilisearch](https://img.shields.io/badge/Meilisearch-FA8072)
[![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=fff)](https://github.com/D4M13N-D3V/meilisearch.NET)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

MeiliSearch .NET Integration is a NuGet package that seamlessly embeds MeiliSearch into your C# application. It manages the background process and health checks for you, simplifying the integration of full-text search capabilities. In future updates, it will also handle automatic compression and decompression of indexes to help manage local storage usage effectively.

## Features

- [x] **Embedded MeiliSearch**: Integrate MeiliSearch directly into your application.
- [x] **Manage Indexes**: Manage your indexs and documents through the SDK, you can still use the default Meilisearch SDK.
- [x] **Add Documents**: Ability to add documents and have validation on if the index is loaded.
- [x] **Background Process Management**: Automatically handles the lifecycle of the MeiliSearch process.
- [x] **Health Monitoring**: Regular checks on the health of the MeiliSearch instance to ensure it stays running.
- [x] **API Key Management**: A cryptographically-secure master key is generated each time the service starts, unless you opt in to a custom key in configuration.
- [ ] **Resource Monitoring**: Monitor the resources being used including storage by your MeiliSearch.
- [ ] **Future Index Management**: Upcoming feature to automatically compress and decompress indexes for optimized local storage.
- [ ] **Caching Mechanism**: Cache the comrpessed indexes so they are returned when you ask for a list of all indexs.
- [ ] **Search Capabilities**: Ability to use the meilisearch native search capabilities with the index being loaded validation.
- [ ] **Embedded Ollama**: Intergated Ollama directly into your application with a configured model.
- [ ] **AI Search Capabilities**: Ability to use the meilisearch native AI search capabilities with the index being loaded validation.

## Installation

To add the MeiliSearch .NET Integration package to your project, you can install it directly from NuGet. Follow the steps below based on your preferred method:

### Package Manager Console

Open the Package Manager Console in Visual Studio and run the following command:

```bash
Install-Package meilisearch.NET
```

### .NET CLI

If you're using the .NET CLI, run the following command in your terminal:

```bash
dotnet add package meilisearch.NET
```

## Configuration

The service reads its settings from the `Meili` section of your configuration via the
`MeiliSearchConfiguration` class. The following options are available:

- **Port**: The port on which Meilisearch will run (e.g. `7700`).
- **CustomApiKey**: When `true`, the value of `ApiKey` is used as the Meilisearch master key.
  When `false` (the default behaviour), a fresh cryptographically-secure key is generated on
  each start.
- **ApiKey**: The master key to use when `CustomApiKey` is `true`. The service throws on startup
  if `CustomApiKey` is enabled but this value is empty. Meilisearch runs in `production` mode,
  which requires a key of at least 16 bytes.
- **UiEnabled**: Reserved for future use.

Configure these in your `appsettings.json`:

```json
{
  "Meili": {
    "Port": 7700,
    "UiEnabled": true,
    "CustomApiKey": false,
    "ApiKey": ""
  }
}
```

## Usage

Register the service with dependency injection. `AddMeiliSearchService()` registers
`MeilisearchService` as a singleton **and** as an `IHostedService`, so the embedded Meilisearch
process is started/stopped automatically with the host lifecycle — you do not start it manually.

```csharp
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using meilisearch.NET.Extensions;

ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddMeiliSearchService();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();
await app.RunAsync();
```

Resolve `MeilisearchService` from DI (for example in your own `IHostedService` registered after
`AddMeiliSearchService()`, so the server is already running) and use the async API below.

## MeilisearchService Class Usage Guide

### Methods

#### CreateIndexAsync

Creates a new index whose filterable attributes are derived from `T`.

```csharp
await service.CreateIndexAsync<MyDocument>("my_index");
```

#### DeleteIndexAsync

Deletes an existing index with the specified name.

```csharp
await service.DeleteIndexAsync("my_index");
```

#### AddDocument

Queues a document for the specified index. Queued documents are flushed to the server in batches
and on graceful shutdown. `IDocument` is an interface with a `Guid Id`:

```csharp
public class MyDocument : IDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

var document = new MyDocument { Id = Guid.NewGuid(), Title = "My Document" };
service.AddDocument("my_index", document);
```

#### GetAllIndexesAsync

Retrieves the list of indexes created through the SDK.

```csharp
List<string> indexes = await service.GetAllIndexesAsync();
```

#### RestartAsync / Stop

Restart or stop the embedded process directly if you need to:

```csharp
await service.RestartAsync();
service.Stop();
```

### Status

`Status` reports the current lifecycle state of the embedded process
(`Stopped`, `Starting`, `Running`, `Stopping`, `Crashed`):

```csharp
MeiliSearchStatus status = service.Status;
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please feel free to submit issues, pull requests, or suggestions to improve this project.

## Support

For any issues or questions, please open an issue on the [GitHub repository](https://github.com/D4M13N-D3V/meilisearch.NET/issues).
