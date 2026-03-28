namespace RetrievalBenchmark.Core;

public interface IRetrievalBackend
{
    string Name { get; }
    Task<RetrievalResult> RetrieveAsync(RetrievalRequest request);
    Task<CorpusStatus> GetCorpusStatusAsync();
}
