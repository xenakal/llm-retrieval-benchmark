namespace RetrievalBenchmark.Core;

public record EvalQuery(
    string QueryId,
    string Text,
    QueryClass Class,
    List<string> RelevantDocIds,
    List<string>? IrrelevantDocIds = null);
