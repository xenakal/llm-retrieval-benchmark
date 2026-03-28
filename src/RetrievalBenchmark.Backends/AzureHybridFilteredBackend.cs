using Azure.Search.Documents;

namespace RetrievalBenchmark.Backends;

public class AzureHybridFilteredBackend(
    SearchClient searchClient,
    string docIdFieldName,
    string vectorFieldName)
    : AzureSearchBackendBase(searchClient, docIdFieldName, vectorFieldName)
{
    public override string Name => "AzureHybridFiltered";
    protected override AzureSearchMode SearchMode => AzureSearchMode.Hybrid;
    protected override bool ApplyMetadataFilters => true;
}
