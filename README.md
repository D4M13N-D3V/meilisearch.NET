# MeiliSearch .NET Embedded

[![NuGet Version](https://img.shields.io/nuget/v/YourPackageName.svg)](https://www.nuget.org/packages/YourPackageName)  
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

MeiliSearch .NET Integration is a NuGet package that seamlessly embeds MeiliSearch into your C# application. It manages the background process and health checks for you, simplifying the integration of full-text search capabilities. In future updates, it will also handle automatic compression and decompression of indexes to help manage local storage usage effectively.

## Features

- **Embedded MeiliSearch**: Integrate MeiliSearch directly into your application.
- **Background Process Management**: Automatically handles the lifecycle of the MeiliSearch process.
- **Health Monitoring**: Regular checks on the health of the MeiliSearch instance.
- **API Key Management**: An API key is automatically regenerated every time the MeiliSearch service starts unless one is specified in the configuration.
- **Resource Monitoring**: Monitor the resources being used including storage by your MeiliSearch.
- **Future Index Management**: Upcoming feature to automatically compress and decompress indexes for optimized local storage.

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