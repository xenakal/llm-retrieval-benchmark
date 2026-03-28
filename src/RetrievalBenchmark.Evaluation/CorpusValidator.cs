using RetrievalBenchmark.Core;

namespace RetrievalBenchmark.Evaluation;

public static class CorpusValidator
{
    /// <summary>
    /// Asserts that the backend's indexed corpus matches the canonical manifest.
    /// Throws if doc counts differ or any manifest doc ID is missing from the backend.
    /// </summary>
    public static void Validate(CorpusManifest manifest, CorpusStatus status)
    {
        if (status.DocCount != manifest.Documents.Count)
            throw new InvalidOperationException(
                $"Corpus doc count mismatch: manifest has {manifest.Documents.Count}, backend reports {status.DocCount}.");

        var backendIds = new HashSet<string>(status.DocIds);
        var missingIds = manifest.Documents
            .Select(d => d.DocId)
            .Where(id => !backendIds.Contains(id))
            .ToList();

        if (missingIds.Count > 0)
            throw new InvalidOperationException(
                $"Backend is missing {missingIds.Count} manifest document(s): {string.Join(", ", missingIds)}.");
    }
}
