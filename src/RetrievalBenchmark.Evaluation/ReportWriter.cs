using System.Text.Json;
using System.Text.Json.Serialization;
using RetrievalBenchmark.Core;

namespace RetrievalBenchmark.Evaluation;

public class ReportWriter(string outputDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes the report to JSON and writes it to the output directory.
    /// Filename format: {backendName}_{timestamp:yyyyMMdd_HHmmss}.json
    /// </summary>
    public async Task WriteAsync(EvalReport report)
    {

        var json = JsonSerializer.Serialize(report, JsonOptions);
        var fileName = $"{report.BackendName}_{report.Timestamp:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(outputDirectory, fileName);

        await File.WriteAllTextAsync(filePath, json);
    }
}
