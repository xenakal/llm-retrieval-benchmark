namespace RetrievalBenchmark.Core;

public record EvalReport(
    string BackendName,
    DateTimeOffset Timestamp,
    ReportCorpusInfo CorpusInfo,
    Dictionary<QueryClass, QueryClassMetrics> MetricsByClass,
    List<QueryDetail> QueryDetails);

public record ReportCorpusInfo(
    int DocCount,
    string ManifestHash);

public record QueryClassMetrics(
    QueryClass Class,
    int QueryCount,
    double AveragePrecisionAtK,
    double AverageRecallAtK,
    double MeanReciprocalRank,
    double AverageLatencyMs,
    decimal AverageCost);

public record QueryDetail(
    string QueryId,
    QueryClass Class,
    List<ReturnedDocResult> ReturnedDocs,
    double PrecisionAtK,
    double RecallAtK,
    double ReciprocalRank,
    double LatencyMs,
    decimal Cost);

public record ReturnedDocResult(string DocId, double Score);
