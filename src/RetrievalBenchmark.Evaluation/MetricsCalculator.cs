namespace RetrievalBenchmark.Evaluation;

public static class MetricsCalculator
{
    /// <summary>Of the top-k returned docs, what fraction are relevant?</summary>
    public static double PrecisionAtK(List<string> returnedDocIds, List<string> relevantDocIds, int k)
    {
        var denominator = Math.Min(k, returnedDocIds.Count);
        if (denominator == 0 || relevantDocIds.Count == 0) return 0.0;
        var relevantSet = new HashSet<string>(relevantDocIds);
        var hits = returnedDocIds.Take(k).Count(id => relevantSet.Contains(id));
        return (double)hits / denominator;
    }

    /// <summary>Of all relevant docs, what fraction appear in the top-k returned?</summary>
    public static double RecallAtK(List<string> returnedDocIds, List<string> relevantDocIds, int k)
    {
        if (relevantDocIds.Count == 0) return 0.0;
        var relevantSet = new HashSet<string>(relevantDocIds);
        var hits = returnedDocIds.Take(k).Count(id => relevantSet.Contains(id));
        return (double)hits / relevantDocIds.Count;
    }

    /// <summary>Reciprocal of the rank of the first relevant result; 0 if none found.</summary>
    public static double MeanReciprocalRank(List<string> returnedDocIds, List<string> relevantDocIds)
    {
        var relevantSet = new HashSet<string>(relevantDocIds);
        for (var i = 0; i < returnedDocIds.Count; i++)
        {
            if (relevantSet.Contains(returnedDocIds[i]))
                return 1.0 / (i + 1);
        }
        return 0.0;
    }
}
