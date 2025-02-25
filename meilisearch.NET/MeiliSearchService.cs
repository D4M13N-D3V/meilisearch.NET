using System.Collections.ObjectModel;
using System.Collections.Specialized;
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


public class MeilisearchService:IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MeilisearchService> _logger;
    public readonly MeilisearchClient Client;
    private readonly MeiliSearchConfiguration _meiliConfiguration;
    private Process process;
    private ObservableCollection<KeyValuePair<string,IDocument>> _documentCollection;
    private const int THRESHOLD = 10000;
    private readonly List<string> FIELDS = new List<string>
    {
        "id",
        "path",
        "createdAtUtc",
        "updatedAtUtc",
        "lastAccessedAtUtc",
        "name",
        "type",
        "ext",
        "size"
    };

    public MeilisearchService(HttpClient httpClient, ILogger<MeilisearchService> logger, MeiliSearchConfiguration meiliConfiguration)
    {
        _httpClient = httpClient;
        _meiliConfiguration = meiliConfiguration;
        _logger = logger;
        Client = new MeilisearchClient("http://localhost:"+meiliConfiguration.MeiliPort );
        _documentCollection = new ObservableCollection<KeyValuePair<string,IDocument>>();
        _documentCollection.CollectionChanged += CheckIfNeedDocumentSync;
        Initialize();
    }
    

    
    #region Private

    private async void Initialize()
    {
        await StartMeilisearch();
        EnsureMeilisearchIsRunning();
        EnsureRepositoryIndexExists();
    }
    
    private async void EnsureRepositoryIndexExists()
    {
        Task.Delay(5000).Wait();
        var indexes = Client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid == "index_bindings"))
        {
            _logger.LogInformation("index bindings already exists, skipping creation of index.");
            return;
        }
        _logger.LogInformation("Creating index bindings for SDK to track indexs...");
        Client.CreateIndexAsync("index_bindings").Wait();
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

    private void EnsureMeilisearchIsRunning()
    {
        if (!IsMeilisearchRunning())
        {
            StartMeilisearch().Wait();
        }
    }

    private bool IsMeilisearchRunning()
    {
        var processName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "meilisearch-windows" 
            : "meilisearch";
        var processes = Process.GetProcessesByName(processName);
        return processes.Any();
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

        // Set execute permissions on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var chmod = Process.Start("chmod", $"+x {binaryPath}");
                chmod?.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to set execute permissions on binary: {ex.Message}");
            }
        }
        var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "localhost" 
            : "127.0.0.1";
        var args = "--http-addr "+host+":" + _meiliConfiguration.MeiliPort 
                  + " --env development --db-path " 
                  + Path.Combine(AppContext.BaseDirectory, "db");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process = new Process { StartInfo = processStartInfo };
        
        try
        {
            process.Start();
            await Task.Delay(5000); // Wait for the process to start
            _logger.LogInformation($"Started Meilisearch process using binary: {binaryName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start Meilisearch: {ex.Message}");
            throw;
        }
    }
    
    private void CheckIfNeedDocumentSync(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckIfNeedDocumentSync(THRESHOLD);
    }

    private void CheckIfNeedDocumentSync(int? threshold = null)
    {
        threshold = threshold ?? 0;
        if(_documentCollection.Count>=threshold)
        {
            _logger.LogInformation("Threshold reached, syncing metadata to server.");
            var grouped = _documentCollection.GroupBy(pair => pair.Key)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToList());
            foreach (var repository in grouped)
            {
                var repositoryIndex = Client.GetIndexAsync(repository.Key).Result;
                var documents = _documentCollection.ToList();
                _documentCollection.Clear();
                var result = RetryAsync(() => repositoryIndex.AddDocumentsAsync(repository.Value, "id")).Result;
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
    #endregion
    
    #region Public
    /// <summary>
    /// Creates a new index on the Meilisearch server if it does not already exist.
    /// </summary>
    /// <param name="indexName">The name for the new index.</param>
    public void CreateIndex(string indexName)
    {
        var indexes = Client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid == indexName))
        {
            _logger.LogWarning($"Index {indexName} already exists, skipping creation of index.");
            return;
        }
        _logger.LogTrace($"Creating index '{indexName}'...");
        Client.CreateIndexAsync(indexName).Wait();
        Task.Delay(5000).Wait();
        var index = Client.GetIndexAsync(indexName).Result;
        var test = index.GetFilterableAttributesAsync().Result;
        index.UpdateFilterableAttributesAsync(FIELDS).Wait();
        _logger.LogInformation($"{indexName} index created!");
        
        Client.GetIndexAsync("index_bindings").Result.AddDocumentsAsync(new List<Models.Index>
        {
            new Models.Index()
            {
                Name = indexName
            }
        }, "name").Wait();
    }

    /// <summary>
    /// Deletes a index for a repoistory.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    public void DeleteIndex(string indexName)
    {
        var indexes = Client.GetAllIndexesAsync().Result;
        if (indexes.Results.Any(x => x.Uid != indexName))
        {
            _logger.LogWarning($"Index '{indexName}' does not exist, skipping deletion of index.");
            return;
        }
        _logger.LogTrace($"Deleting index '{indexName}'...");
        Client.DeleteIndexAsync(indexName).Wait();
        Client.GetIndexAsync("index_bindings").Result.DeleteOneDocumentAsync(indexName).Wait();
        _logger.LogInformation($"Deleted index '{indexName}'!");
    }
    
    public void AddDocument(string repositoryId, IDocument document)
    {
        _logger.LogTrace($"Adding document '{document.Id}' to repository '{repositoryId}'...");
        _documentCollection.Add(new KeyValuePair<string, IDocument>(repositoryId, document));
        _logger.LogInformation($"Document {document.Id} added to collection.");
    }

    public List<string> GetAllIndexes()
    {
        _logger.LogTrace("Fetching all indexes from Meilisearch server created with the SDK...");
        var result = Client.GetAllIndexesAsync().Result.Results.Select(x => x.Uid).Where(x=>x!="index_bindings").ToList();
        _logger.LogInformation($"Fetched {result.Count} indexes from Meilisearch server.");
        return  result;
    }

    public async void Start()
    {
        await StartMeilisearch();
    }

    public async void Stop()
    {
        process.Kill();
    }
    
    public void Dispose()
    {
        CheckIfNeedDocumentSync();
        Stop();
        _httpClient.Dispose();
    }
    #endregion
}