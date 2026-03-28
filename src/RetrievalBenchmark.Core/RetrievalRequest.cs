namespace RetrievalBenchmark.Core;

public record RetrievalRequest(
    string Query,
    int TopK = 10,
    Dictionary<string, string>? Filters = null);
