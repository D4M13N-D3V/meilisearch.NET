using meilisearch.NET.Interfaces;

namespace meilisearch.NET.example;

public class document:IDocument
{
    public string message { get; set; }
}