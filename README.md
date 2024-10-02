# MeiliSearch .NET Embedded

[![NuGet Version](https://img.shields.io/nuget/v/YourPackageName.svg)](https://www.nuget.org/packages/YourPackageName)  
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

MeiliSearch .NET Integration is a NuGet package that seamlessly embeds MeiliSearch into your C# application. It manages the background process and health checks for you, simplifying the integration of full-text search capabilities. In future updates, it will also handle automatic compression and decompression of indexes to help manage local storage usage effectively.

## Features

[X] - **Embedded MeiliSearch**: Integrate MeiliSearch directly into your application.
[X] - **Background Process Management**: Automatically handles the lifecycle of the MeiliSearch process.
[X] - **Health Monitoring**: Regular checks on the health of the MeiliSearch instance.
[X] - **API Key Management**: An API key is automatically regenerated every time the MeiliSearch service starts unless one is specified in the configuration.
[ ] - **Resource Monitoring**: Monitor the resources being used including storage by your MeiliSearch.
[ ] - **Future Index Management**: Upcoming feature to automatically compress and decompress indexes for optimized local storage.

## Installation

To add the MeiliSearch .NET Integration package to your project, use the following command in the Package Manager Console:

```bash
Install-Package YourPackageName
```

Or, if you're using the .NET CLI:

```bash
dotnet add package YourPackageName
```

## Configuration

To configure the MeiliSearch service, add the following section to your `appsettings.json` file:

```json
{
  ...
  "Meili": {
    "Port": 7700,
    "UiEnabled": true,
    "CustomApiKey": false,
    "ApiKey": "YourOptionalApiKey" // Specify this if you want a fixed API key
  },
  ...
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft.AspNetCore": "Trace"
    }
  },
  "AllowedHosts": "*"
}
```

### AppSettings Options

- **Port**: The port on which MeiliSearch will run (default is `7700`).
- **UiEnabled**: A boolean value to enable or disable the MeiliSearch UI (default is `true`).
- **ApiKey**: An optional API key. If specified, this key will be used; otherwise, a new key will be generated each time the service starts.

## Usage

To set up the MeiliSearch service in your application, you'll need to configure dependency injection. Here’s an example of how to do that:

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

### MeiliSearchService Class Usage Guide

This class is designed to manage the lifecycle of a MeiliSearch process. It provides methods to start, stop, and restart the MeiliSearch process.

#### Methods

##### Start
This method is used to start the MeiliSearch process. It logs the start of the process, sets the status to **Starting**, and attempts to start the process. If the process starts successfully, the status is set to **Running** and a success message is logged. If it fails to start, an error message is logged and the exception is rethrown.

```csharp
MeiliSearchService service = new MeiliSearchService();
service.Start();
```

##### Stop
This method is used to stop the MeiliSearch process. It first checks if the process is running. If not, it logs a warning and throws an exception. If the process is running, it logs the stop of the process, sets the status to **Stopping**, and attempts to stop the process. If the process stops successfully, the status is set to **Stopped** and a success message is logged.

```csharp
service.Stop();
```

##### Restart
This method is used to restart the MeiliSearch process. It logs the restart of the process, stops the process using the **Stop** method, and starts the process using the **Start** method.

```csharp
service.Restart();
```

#### Status
The **Status** property indicates the current status of the MeiliSearch process. It can be one of the following values:
- **Starting**: The process is in the process of starting.
- **Running**: The process is currently running.
- **Stopping**: The process is in the process of stopping.
- **Stopped**: The process is currently stopped.

```csharp
MeiliSearchStatus status = service.Status;
```

> **Note**: Please note that you should handle exceptions appropriately when using these methods, as they may throw exceptions if the process fails to start or stop.


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please feel free to submit issues, pull requests, or suggestions to improve this project.

## Support

For any issues or questions, please open an issue on GitHub or contact us via [your contact method].

---

Feel free to customize this README as necessary for your package, especially regarding the project name and license details!
```

This revision includes details about the API key regeneration and how to specify a fixed API key if desired.
