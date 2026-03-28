using System.Diagnostics;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using RetrievalBenchmark.Core;

namespace RetrievalBenchmark.Backends;

public abstract class AzureSearchBackendBase(
    SearchClient _searchClient,
    string _docIdFieldName,
    string _vectorFieldName,
    string? _semanticConfigurationName = null) : IRetrievalBackend
{
    public abstract string Name { get; }
    protected abstract AzureSearchMode SearchMode { get; }
    protected abstract bool ApplyMetadataFilters { get; }

    public async Task<RetrievalResult> RetrieveAsync(RetrievalRequest request)
    {
        // Semantic ranker requires k=50 as input to rerank from
        var knn = SearchMode == AzureSearchMode.HybridSemantic ? 50 : request.TopK;

        var options = new SearchOptions
        {
            Size = request.TopK,
            Select = { _docIdFieldName },
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizableTextQuery(request.Query)
                    {
                        KNearestNeighborsCount = knn,
                        Fields = { _vectorFieldName }
                    }
                }
            }
        };

        if (SearchMode == AzureSearchMode.HybridSemantic)
        {
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _semanticConfigurationName
            };
        }

        if (ApplyMetadataFilters && request.Filters?.Count > 0)
            options.Filter = BuildODataFilter(request.Filters);

        var stopwatch = Stopwatch.StartNew();
        var response = await _searchClient.SearchAsync<SearchDocument>(request.Query, options);
        stopwatch.Stop();

        var documents = new List<RetrievedDocument>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var docId = result.Document[_docIdFieldName]?.ToString() ?? string.Empty;
            var score = SearchMode == AzureSearchMode.HybridSemantic
                ? result.SemanticSearch?.RerankerScore ?? result.Score ?? 0.0
                : result.Score ?? 0.0;
            documents.Add(new RetrievedDocument(docId, score));
        }

        return new RetrievalResult(documents, stopwatch.Elapsed, EstimateCost());
    }

    public async Task<CorpusStatus> GetCorpusStatusAsync()
    {
        // Size is capped at 1000 — sufficient for the expected corpus size
        var options = new SearchOptions
        {
            Select = { _docIdFieldName },
            Size = 1000,
            IncludeTotalCount = true
        };

        var response = await _searchClient.SearchAsync<SearchDocument>("*", options);

        var docIds = new List<string>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var docId = result.Document[_docIdFieldName]?.ToString();
            if (docId is not null) docIds.Add(docId);
        }

        var totalCount = (int)(response.Value.TotalCount ?? docIds.Count);
        return new CorpusStatus(totalCount, docIds, string.Empty);
    }

    private decimal EstimateCost() => SearchMode switch
    {
        AzureSearchMode.HybridSemantic => 0.0015m,  // hybrid + semantic ranker
        _ => 0.0005m                                  // hybrid only
    };

    private static string BuildODataFilter(Dictionary<string, string> filters)
    {
        var clauses = filters.Select(kvp =>
            $"{kvp.Key} eq '{kvp.Value.Replace("'", "''")}'");
        return string.Join(" and ", clauses);
    }
}
