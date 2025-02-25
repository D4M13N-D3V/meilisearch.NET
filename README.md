# MeiliSearch .NET Embedded

![Gitea Release](https://img.shields.io/gitea/v/release/damien/meilisearch.NET?gitea_url=https://git.d4m13n.dev)
![Dotnet 8](https://img.shields.io/badge/-.NET%208.0-blueviolet?logo=dotnet)
![Meilisearch](https://img.shields.io/badge/Meilisearch-FA8072)
![Ollama](https://img.shields.io/badge/Ollama-Powered-orange)
[![Gitea](https://img.shields.io/badge/Gitea-6eaa5b?logo=gitea&logoColor=fff)](#)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

MeiliSearch .NET Integration is a NuGet package that seamlessly embeds MeiliSearch into your C# application. It manages the background process and health checks for you, simplifying the integration of full-text search capabilities. In future updates, it will also handle automatic compression and decompression of indexes to help manage local storage usage effectively.

## Features

- [x] **Embedded MeiliSearch**: Integrate MeiliSearch directly into your application.
- [x] **Manage Indexes**: Manage your indexs and documents through the SDK, you can still use the default Meilisearch SDK.
- [x] **Add Documents**: Ability to add documents and have validation on if the index is loaded.
- [x] **Background Process Management**: Automatically handles the lifecycle of the MeiliSearch process.
- [x] **Health Monitoring**: Regular checks on the health of the MeiliSearch instance to ensure it stays running.
- [x] **API Key Management**: An API key is automatically regenerated every time the MeiliSearch service starts unless one is specified in the configuration.
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

## AppSettings Options

- **Port**: The port on which MeiliSearch will run (default is `7700`).
- **UiEnabled**: A boolean value to enable or disable the MeiliSearch UI (default is `true`).
- **ApiKey**: An optional API key. If specified, this key will be used; otherwise, a new key will be generated each time the service starts.

## Configuration

The MeiliSearch service can be configured using the `MeiliSearchConfiguration` class. The following options are available:

- **Port**: The port on which MeiliSearch will run (default is `7700`).
- **UiEnabled**: A boolean value to enable or disable the MeiliSearch UI (default is `true`).
- **ApiKey**: An optional API key. If specified, this key will be used; otherwise, a new key will be generated each time the service starts.

You can configure these options in your `appsettings.json` file as follows:

```json
{
  "MeiliSearch": {
    "Port": 7700,
    "UiEnabled": true,
    "ApiKey": "your_api_key"
  }
}
```

## Usage

To set up the MeiliSearch service in your application, configure dependency injection as shown below:

```csharp
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddMeiliSearchService();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Services.AddLogging();

var app = builder.Build();
app.Run();
Console.ReadLine();
```

## MeiliSearchService Class Usage Guide

### Methods

#### Start

Starts the MeiliSearch process. Logs the start of the process, sets the status to **Starting**, and attempts to start the process.

```csharp
MeiliSearchService service = new MeiliSearchService();
service.Start();
```

#### Stop

Stops the MeiliSearch process. Logs the stop of the process, sets the status to **Stopping**, and attempts to stop the process.

```csharp
service.Stop();
```

#### Restart

Restarts the MeiliSearch process. Stops the process using the **Stop** method and starts it using the **Start** method.

```csharp
service.Restart();
```

#### CreateIndex

Creates a new index with the specified name.

```csharp
service.CreateIndex("my_index");
```

#### DeleteIndex

Deletes an existing index with the specified name.

```csharp
service.DeleteIndex("my_index");
```

#### AddDocument

Adds a document to the specified index.

```csharp
public class MyDocument : IDocument
{
    public string Id { get; set; }
    public string Title { get; set; }
}

var document = new MyDocument { Id = "1", Title = "My Document" };
service.AddDocument("my_index", document);
```

#### GetAllIndexes

Retrieves a list of all existing indexes.

```csharp
List<string> indexes = service.GetAllIndexes();
```

### Status

Indicates the current status of the MeiliSearch process.

```csharp
MeiliSearchStatus status = service.Status;
```

## Notes
https://github.com/Mozilla-Ocho/llamafile


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please feel free to submit issues, pull requests, or suggestions to improve this project.

## Support

For any issues or questions, please open an issue on GitHub or contact us via [your contact method].

---

Feel free to customize this README as necessary for your package, especially regarding the project name and license details!

---
