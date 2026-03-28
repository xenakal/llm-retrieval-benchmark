using RetrievalBenchmark.Core;

namespace RetrievalBenchmark.Evaluation;

public class EvalRunner
{
    /// <summary>
    /// Validates the backend's corpus, runs all queries, computes per-query and
    /// per-class metrics, and returns a fully populated EvalReport.
    /// </summary>
    public async Task<EvalReport> RunAsync(
        IRetrievalBackend backend,
        EvalDataset dataset,
        CorpusManifest manifest)
    {
        var status = await backend.GetCorpusStatusAsync();
        CorpusValidator.Validate(manifest, status);

        var queryDetails = new List<QueryDetail>();

        foreach (var query in dataset.Queries)
        {
            var request = new RetrievalRequest(query.Text);
            var result = await backend.RetrieveAsync(request);

            var returnedDocIds = result.Documents.Select(d => d.DocId).ToList();
            var returnedDocs = result.Documents
                .Select(d => new ReturnedDocResult(d.DocId, d.Score))
                .ToList();

            queryDetails.Add(new QueryDetail(
                query.QueryId,
                query.Class,
                returnedDocs,
                PrecisionAtK: MetricsCalculator.PrecisionAtK(returnedDocIds, query.RelevantDocIds, request.TopK),
                RecallAtK: MetricsCalculator.RecallAtK(returnedDocIds, query.RelevantDocIds, request.TopK),
                ReciprocalRank: MetricsCalculator.MeanReciprocalRank(returnedDocIds, query.RelevantDocIds),
                LatencyMs: result.Latency.TotalMilliseconds,
                Cost: result.EstimatedCost));
        }

        var metricsByClass = queryDetails
            .GroupBy(qd => qd.Class)
            .ToDictionary(
                g => g.Key,
                g => new QueryClassMetrics(
                    g.Key,
                    QueryCount: g.Count(),
                    AveragePrecisionAtK: g.Average(qd => qd.PrecisionAtK),
                    AverageRecallAtK: g.Average(qd => qd.RecallAtK),
                    MeanReciprocalRank: g.Average(qd => qd.ReciprocalRank),
                    AverageLatencyMs: g.Average(qd => qd.LatencyMs),
                    AverageCost: g.Average(qd => qd.Cost)));

        return new EvalReport(
            backend.Name,
            DateTimeOffset.UtcNow,
            new ReportCorpusInfo(status.DocCount, status.ManifestHash),
            metricsByClass,
            queryDetails);
    }
}
