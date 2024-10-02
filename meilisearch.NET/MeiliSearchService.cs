using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using Meilisearch;
using meilisearch.NET.Configurations;
using meilisearch.NET.Enums;
using meilisearch.NET.Extensions;
using meilisearch.NET.Interfaces;
using Microsoft.Extensions.Logging;
using Index = Meilisearch.Index;

namespace meilisearch.NET;


public class MeiliSearchService:IMeiliSearchService
{

    public MeilisearchClient Sdk { get; set; }
    public MeiliSearchStatus Status { get; set; }
    
    private readonly MeiliSearchConfiguration _meiliSearchConfiguration;
    private readonly ProcessStartInfo _processStartInfo;
    private readonly ILogger<MeiliSearchService> _logger;
    private readonly HttpClient _sdkHttpClient;

    private Timer _healthCheckTimer;
    private Process? _process;
    
    public MeiliSearchService(HttpClient httpClient, ILogger<MeiliSearchService> logger, MeiliSearchConfiguration meiliSearchConfiguration)
    {
        _logger = logger;
        _meiliSearchConfiguration = meiliSearchConfiguration;
        var binaryName = GetBinaryName();
        MakeExecutable(Path.Combine(AppContext.BaseDirectory, binaryName));
        string apiKey = "";
        if (_meiliSearchConfiguration.EnableCustomApiKey)
            apiKey = _meiliSearchConfiguration.ApiKey;
        else
            apiKey = ApiKeyGenerator.GenerateApiKey();
        
        _sdkHttpClient = httpClient;
        _sdkHttpClient.BaseAddress = new Uri("http://localhost:"+meiliSearchConfiguration.MeiliPort);
        Sdk = new MeilisearchClient(httpClient, apiKey);
        var path = Path.Combine(AppContext.BaseDirectory, binaryName);
        var args = "--http-addr 127.0.0.1:" + _meiliSearchConfiguration.MeiliPort 
                        + " --master-key "+apiKey
                        + " --env "+(_meiliSearchConfiguration.UiEnabled ? "development" : "production")
                        +" --db-path "+GetDataLocation();
        _processStartInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        Status = MeiliSearchStatus.Stopped;
        _logger?.LogInformation($"Meilisearch service initialized with the UI {(_meiliSearchConfiguration.UiEnabled ? "Enabled" : "Disabled)")} with database located at {GetDataLocation()}.");
    }



    private static void MakeExecutable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' does not exist.", filePath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // For Linux and macOS, use chmod +x
            ExecuteCommand($"chmod +x \"{filePath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
        }
        else
        {
            throw new PlatformNotSupportedException("This platform is not supported.");
        }

        Console.WriteLine($"File '{filePath}' has been made executable.");
    }
    private static void ExecuteCommand(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash", // Use bash for Linux and macOS
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processStartInfo))
        {
            process.WaitForExit();

            // Optionally handle output and errors
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Error executing command: {error}");
            }
        }
    }
    private static string GetDataLocation()
    {
        return Path.Combine(AppContext.BaseDirectory, "data");
        string appDataDirectory;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeiliSearchEmbedded");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "MeiliSearchEmbedded");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "MeiliSearchEmbedded");
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS.");
        }
        return appDataDirectory;
    }
    private static string GetBinaryName()
    {
        string os = RuntimeInformation.OSDescription.ToLowerInvariant();
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string platform;

        if (os.Contains("linux"))
        {
            platform = "meilisearch-linux-" + (architecture.Contains("arm") ? "arm" : "x64");
        }
        else if (os.Contains("macos") || os.Contains("darwin"))
        {
            platform = "meilisearch-macos-" + (architecture.Contains("arm") ? "arm" : "x64");
        }
        else if (os.Contains("windows"))
        {
            platform = "meilisearch-windows.exe";
        }
        else
        {
            throw new PlatformNotSupportedException("Operating system not supported.");
        }

        return platform;
    }
    private bool MeilisSearchReachable()
    {
        string host = "127.0.0.1"; // localhost
        int port = _meiliSearchConfiguration.MeiliPort;
        try
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(host, port);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
    
    public bool HealthCheck(object? state)
    {
        if (_process == null)
            throw new Exception("Meilisearch process is not running.");
        var responding = _process.Responding;
        var exited = _process.HasExited || _process.ExitCode == 0;
        var reachable = MeilisSearchReachable();
        return responding && exited && reachable;
    }
    
    public void RefreshApiKey()
    {
        _logger.LogInformation("Refreshing API key.");
        string apiKey = "";
        if (_meiliSearchConfiguration.EnableCustomApiKey)
            apiKey = _meiliSearchConfiguration.ApiKey;
        else
            apiKey = ApiKeyGenerator.GenerateApiKey();
        Sdk = new MeilisearchClient(_sdkHttpClient, apiKey);
        _logger.LogInformation("New API key generated.");
        Restart();
    }

    public void Start()
    {
        _logger.LogInformation("Starting MeiliSearch process.");
        Status = MeiliSearchStatus.Starting;
        _process = new Process { StartInfo = _processStartInfo };
        
        try
        {
            _process.Start();
            Status = MeiliSearchStatus.Running;
            _logger.LogInformation("MeiliSearch process started successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MeiliSearch process.");
            throw;
        }
    }

    public void Restart()
    {
        _logger.LogInformation("Restarting MeiliSearch process.");
        Stop();
        Start();
    }

    public void Stop()
    {
        if (_process == null)
        {
            _logger.LogWarning("Attempted to stop MeiliSearch process, but it is not running.");
            throw new Exception("Meilisearch process is not running.");
        }

        _logger.LogInformation("Stopping MeiliSearch process.");
        Status = MeiliSearchStatus.Stopping;

        try
        {
            _process.Kill();
            Status = MeiliSearchStatus.Stopped;
            _logger.LogInformation("MeiliSearch process stopped successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop MeiliSearch process.");
            throw;
        }
    }

    public List<string> ListIndexs()
    {
        throw new NotImplementedException();
    }

    public Index CreateIndex()
    {
        throw new NotImplementedException();
    }

    public void LoadIndex(string indexId)
    {
        throw new NotImplementedException();
    }

    public void UnloadIndex(string indexId)
    {
        throw new NotImplementedException();
    }

    public void DeleteIndex(string indexId)
    {
        throw new NotImplementedException();
    }
}