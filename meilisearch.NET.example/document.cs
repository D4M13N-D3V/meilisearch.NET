using meilisearch.NET.Interfaces;

namespace meilisearch.NET.example;

public class Document : IDocument
{
    public Guid Id { get; set; }
    public string message { get; set; } = string.Empty;
}
