using Meilisearch;
using meilisearch.NET.Enums;

namespace meilisearch.NET.Interfaces;

public interface IMeiliSearchService
{
    /// <summary>
    /// Checks if the Meilisearch server is healthy.
    /// </summary>
    /// <returns>True if healthy, false if not.</returns>
    public bool HealthCheck(object? state);
    
    /// <summary>
    /// This is the SDK for Meilisearch. It is used to interact with the Meilisearch server.
    /// </summary>
    public MeilisearchClient Sdk { get; set; }
    
    /// <summary>
    /// This is the status of the Meilisearch server. It is used to check if the server is running or not.
    /// </summary>
    public MeiliSearchStatus Status { get; set; }

    /// <summary>
    /// Refreshes the API key. This is useful if you want to change the API key for security reasons.
    /// </summary>
    public void RefreshApiKey();
    
    /// <summary>
    /// This will start the Meilisearch server. This is useful if you want to start the server without starting the application.
    /// </summary>
    public void Start();
    
    /// <summary>
    /// This will restart the Meilisearch server. This is useful if you have made changes to the server and want to apply them without restarting the application.
    /// </summary>
    public void Restart();

    /// <summary>
    /// This will stop the Meilisearch server. This is useful if you want to stop the server without stopping the application.
    /// </summary>
    public void Stop();
    
    /// <summary>
    /// This will list all of the indexes regardless if they are loaded or unloaded.
    /// </summary>
    /// <returns></returns>
    public List<string> ListIndexs();
    
    /// <summary>
    /// Creates a index and adds a .name file so that we can track the name of the index.
    /// </summary>
    /// <returns></returns>
    public Meilisearch.Index CreateIndex();
    
    /// <summary>
    /// Loads a index and uncompresses it.
    /// </summary>
    /// <param name="indexId">The ID of the index to load.</param>
    public void LoadIndex(string indexId);
    
    /// <summary>
    /// Unloads a index and compresses it. 
    /// </summary>
    /// <param name="indexId">The ID of the index to unload.</param>
    public void UnloadIndex(string indexId);
    
    /// <summary>
    /// This will delete a index and remove the .name file.
    /// </summary>
    /// <param name="indexId">The ID of the index to delete.</param>
    public void DeleteIndex(string indexId);
    
}