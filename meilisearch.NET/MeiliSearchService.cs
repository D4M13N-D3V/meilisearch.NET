using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Meilisearch;
using meilisearch.NET.Configurations;
using meilisearch.NET.Enums;
using meilisearch.NET.Extensions;
using meilisearch.NET.Interfaces;
using Microsoft.Extensions.Logging;
using Index = Meilisearch.Index;

namespace meilisearch.NET;


public class MeilisearchService:IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MeilisearchService> _logger;
    private readonly MeilisearchClient _client;
    private readonly MeiliSearchConfiguration _meiliConfiguration;
    private readonly string _indexBasePath = Path.Combine(AppContext.BaseDirectory, "db", "indexes" );
    private readonly string _apiKey;
    private const int THRESHOLD = 10000;
    private Process? process;
    private volatile bool _stopping;
    private bool _disposed;
    private int _restartAttempts;
    private readonly object _syncLock = new();
    private bool _syncing;
    private ObservableCollection<KeyValuePair<string,IDocument>> _documentCollection;

    public MeilisearchService(HttpClient httpClient, ILogger<MeilisearchService> logger, MeiliSearchConfiguration meiliConfiguration)
    {
        _httpClient = httpClient;
        _meiliConfiguration = meiliConfiguration;
        _logger = logger;
        _apiKey = ResolveApiKey(meiliConfiguration);
        _client = new MeilisearchClient("http://localhost:"+meiliConfiguration.MeiliPort, _apiKey );
        _documentCollection = new ObservableCollection<KeyValuePair<string,IDocument>>();
        _documentCollection.CollectionChanged += CheckIfNeedDocumentSync;
        StartMeilisearch().Wait();
        EnsureRepositoryIndexExists().Wait();
    }
    

    
    #region Private
    private string ResolveApiKey(MeiliSearchConfiguration configuration)
    {
        if (configuration.EnableCustomApiKey)
        {
            if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            {
                throw new InvalidOperationException(
                    "Meili:CustomApiKey is enabled but Meili:ApiKey is empty. " +
                    "Provide a key or disable Meili:CustomApiKey to auto-generate one.");
            }

            _logger.LogInformation("Using configured Meilisearch master key.");
            return configuration.ApiKey;
        }

        _logger.LogInformation("Generating a new Meilisearch master key.");
        return ApiKeyGenerator.GenerateApiKey();
    }
    private async Task EnsureRepositoryIndexExists()
    {
        Task.Delay(5000).Wait();
        var indexes = _client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid == "index_bindings"))
        {
            _logger.LogInformation("index bindings already exists, skipping creation of index.");
            return;
        }
        _logger.LogInformation("Creating index bindings for SDK to track indexs...");
        _client.CreateIndexAsync("index_bindings").Wait();
    }
    
    private string GetMeilisearchBinaryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "meilisearch-windows.exe";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "meilisearch-macos-arm"
                : "meilisearch-macos-x64";
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "meilisearch-linux-arm"
                : "meilisearch-linux-x64";
        }

        throw new PlatformNotSupportedException("Current platform and architecture combination is not supported");
    }

    // Verifies the on-disk binary against the SHA-256 manifest embedded in this
    // assembly before it is ever executed. The manifest lives inside the DLL so
    // it cannot be swapped alongside a tampered binary in the output directory.
    private void VerifyBinary(string binaryName, string binaryPath)
    {
        var expected = GetExpectedChecksum(binaryName);
        if (expected is null)
        {
            throw new SecurityException(
                $"No checksum is recorded for Meilisearch binary '{binaryName}'; refusing to launch an unverified binary.");
        }

        string actual;
        using (var stream = File.OpenRead(binaryPath))
        {
            actual = Convert.ToHexString(SHA256.HashData(stream));
        }

        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError($"Checksum mismatch for {binaryName}: expected {expected}, got {actual}.");
            throw new SecurityException(
                $"Meilisearch binary '{binaryName}' failed integrity verification; refusing to launch.");
        }

        _logger.LogTrace($"Verified integrity of Meilisearch binary '{binaryName}'.");
    }

    private static string? GetExpectedChecksum(string binaryName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("binaries.sha256", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            // Format: "<hex-sha256>  <filename>" (sha256sum style).
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && string.Equals(parts[1], binaryName, StringComparison.Ordinal))
            {
                return parts[0];
            }
        }

        return null;
    }

    private async Task StartMeilisearch()
    {
        var binaryName = GetMeilisearchBinaryName();
        var binaryPath = Path.Combine(AppContext.BaseDirectory, binaryName);
        
        if (!File.Exists(binaryPath))
        {
            _logger.LogError($"Meilisearch binary not found at: {binaryPath}");
            throw new FileNotFoundException($"Could not find Meilisearch binary: {binaryName}");
        }

        VerifyBinary(binaryName, binaryPath);

        // Set execute permissions on Unix-like systems. Uses the managed API
        // (no chmod subprocess, no command-line quoting hazards).
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var current = File.GetUnixFileMode(binaryPath);
                File.SetUnixFileMode(binaryPath,
                    current | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to set execute permissions on binary: {ex.Message}");
            }
        }
        var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "localhost" 
            : "127.0.0.1";
        // The master key is passed via the MEILI_MASTER_KEY environment variable
        // rather than on the command line: process arguments are world-readable
        // (ps / /proc/<pid>/cmdline) and would leak the key to other local users.
        var args = "--http-addr "+host+":" + _meiliConfiguration.MeiliPort
                  + " --env production --db-path "
                  + Path.Combine(AppContext.BaseDirectory, "db");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false,
        };
        processStartInfo.Environment["MEILI_MASTER_KEY"] = _apiKey;

        process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true};
        process.Exited += OnProcessExited;
        try
        {
            process.Start();
            await Task.Delay(5000); // Wait for the process to start
            _restartAttempts = 0; // Successful start resets the backoff.
            _logger.LogInformation($"Started Meilisearch process using binary: {binaryName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start Meilisearch: {ex.Message}");
            throw;
        }
    }

    // Single restart handler with exponential backoff. Does nothing during an
    // intentional shutdown (Stop/Dispose), so the server actually stays down.
    private async void OnProcessExited(object? sender, EventArgs e)
    {
        if (_stopping)
        {
            return;
        }

        var attempt = Interlocked.Increment(ref _restartAttempts);
        var delay = Math.Min(30000, 1000 * (int)Math.Pow(2, Math.Min(attempt - 1, 5)));
        _logger.LogWarning($"Meilisearch process exited unexpectedly. Restarting in {delay}ms (attempt {attempt})...");
        await Task.Delay(delay);

        if (_stopping)
        {
            return;
        }

        try
        {
            await StartMeilisearch();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to restart Meilisearch: {ex.Message}");
        }
    }
    
    private void CheckIfNeedDocumentSync(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckIfNeedDocumentSync(THRESHOLD);
    }

    private void CheckIfNeedDocumentSync(int? threshold = null)
    {
        var effectiveThreshold = threshold ?? 0;

        lock (_syncLock)
        {
            // _syncing guards against re-entrancy: removing synced items below
            // raises CollectionChanged, which calls back into this method.
            if (_syncing || _documentCollection.Count < effectiveThreshold)
            {
                return;
            }

            _syncing = true;
            try
            {
                _logger.LogInformation("Threshold reached, syncing metadata to server.");

                // Snapshot what we sync so documents added during the push are
                // preserved, and only remove items after a successful push (no
                // data loss if the server call fails).
                var snapshot = _documentCollection.ToList();
                var grouped = snapshot
                    .GroupBy(pair => pair.Key)
                    .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToList());

                foreach (var repository in grouped)
                {
                    var repositoryIndex = _client.GetIndexAsync(repository.Key).Result;
                    RetryAsync(() => repositoryIndex.AddDocumentsAsync(repository.Value, "id")).Wait();
                }

                foreach (var item in snapshot)
                {
                    _documentCollection.Remove(item);
                }
            }
            finally
            {
                _syncing = false;
            }
        }
    }
    
    private async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError($"Operation failed after {maxRetries} retries: {ex.Message}");
                    throw;
                }
                _logger.LogWarning($"Operation failed, retrying {retryCount}/{maxRetries}...");
                await Task.Delay(delayMilliseconds);
            }
        }
    }
    public static string[] GetPropertiesInCamelCase<T>()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties
            .Select(p => ToCamelCase(p.Name))
            .ToArray();
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
        {
            return input;
        }

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
    #endregion
    
    #region Public
    public bool IsMeilisearchRunning()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "meilisearch-windows" 
            : "meilisearch";
        var processes = Process.GetProcessesByName(processName);
        return processes.Any();
    }

    public void CreateIndex<T>(string indexName) where T : IDocument
    {
        var indexes = _client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid == indexName))
        {
            _logger.LogWarning($"Index {indexName} already exists, skipping creation of index.");
            return;
        }

        var foldersBefore = Directory.GetDirectories(_indexBasePath);
        _logger.LogTrace($"Creating index '{indexName}'...");
        _client.CreateIndexAsync(indexName).Wait();
        Task.Delay(5000).Wait();
        var index = _client.GetIndexAsync(indexName).Result;
        var test = index.GetFilterableAttributesAsync().Result;
        index.UpdateFilterableAttributesAsync(GetPropertiesInCamelCase<T>()).Wait();
        _logger.LogInformation($"{indexName} index created!");
        var foldersAfter = Directory.GetDirectories(_indexBasePath);
        var folder = Path.GetFileName(foldersAfter.Except(foldersBefore).FirstOrDefault());
        _client.GetIndexAsync("index_bindings").Result.AddDocumentsAsync(new List<Models.Index>
        {
            new()
            {
                Name = indexName,
                CreatedAt = DateTime.UtcNow,
                FolderId = folder
            }
        }, "name").Wait();
    }

    public void DeleteIndex(string indexName)
    {
        var indexes = _client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid == indexName)==false)
        {
            _logger.LogWarning($"Index '{indexName}' does not exist, skipping deletion of index.");
            return;
        }
        _logger.LogTrace($"Deleting index '{indexName}'...");
        _client.DeleteIndexAsync(indexName).Wait();
        _client.GetIndexAsync("index_bindings").Result.DeleteOneDocumentAsync(indexName).Wait();
        _logger.LogInformation($"Deleted index '{indexName}'!");
    }
    
    public void AddDocument(string repositoryId, IDocument document)
    {
        _logger.LogTrace($"Adding document '{document.Id}' to repository '{repositoryId}'...");
        lock (_syncLock)
        {
            _documentCollection.Add(new KeyValuePair<string, IDocument>(repositoryId, document));
        }
        _logger.LogInformation($"Document {document.Id} added to collection.");
    }

    public List<string> GetAllIndexes()
    {
        _logger.LogTrace("Fetching all indexes from Meilisearch server created with the SDK...");
        var result = _client.GetAllIndexesAsync().Result.Results.Select(x => x.Uid).Where(x=>x!="index_bindings").ToList();
        _logger.LogInformation($"Fetched {result.Count} indexes from Meilisearch server.");
        return  result;
    }

    public Task Start()
    {
        _stopping = false;
        return StartMeilisearch();
    }

    public void Stop()
    {
        _stopping = true; // Suppress the auto-restart handler for this shutdown.
        var proc = process;
        if (proc is not { HasExited: false })
        {
            return;
        }

        try
        {
            proc.Kill(entireProcessTree: true);
            proc.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to stop Meilisearch process: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        CheckIfNeedDocumentSync();
        _documentCollection.CollectionChanged -= CheckIfNeedDocumentSync;
        Stop();

        var proc = process;
        if (proc != null)
        {
            proc.Exited -= OnProcessExited;
            proc.Dispose();
        }

        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}