namespace RetrievalBenchmark.Core;

public record RetrievalResult(
    List<RetrievedDocument> Documents,
    TimeSpan Latency,
    decimal EstimatedCost,
    Dictionary<string, object>? Metadata = null);
