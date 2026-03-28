using Azure.Search.Documents;

namespace RetrievalBenchmark.Backends;

public class AzureHybridSemanticBackend(
    SearchClient searchClient,
    string docIdFieldName,
    string vectorFieldName,
    string semanticConfigurationName)
    : AzureSearchBackendBase(searchClient, docIdFieldName, vectorFieldName, semanticConfigurationName)
{
    public override string Name => "AzureHybridSemantic";
    protected override AzureSearchMode SearchMode => AzureSearchMode.HybridSemantic;
    protected override bool ApplyMetadataFilters => false;
}
