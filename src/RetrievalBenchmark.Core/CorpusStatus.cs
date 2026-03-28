namespace RetrievalBenchmark.Core;

public record CorpusStatus(
    int DocCount,
    List<string> DocIds,
    string ManifestHash);
