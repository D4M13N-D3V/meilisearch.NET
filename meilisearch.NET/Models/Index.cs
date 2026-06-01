namespace meilisearch.NET.Models;

public class Index
{
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
