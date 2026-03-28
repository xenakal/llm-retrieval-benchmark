namespace RetrievalBenchmark.Core;

public record CorpusDocument(
    string DocId,
    string Title,
    string FilePath,
    string Checksum,
    Dictionary<string, string> Metadata);
