namespace meilisearch.NET.Models;

public class Index
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string FolderId { get; set; }
}