namespace RetrievalBenchmark.Core;

public record RetrievedDocument(
    string DocId,
    double Score,
    string? ChunkText = null);
