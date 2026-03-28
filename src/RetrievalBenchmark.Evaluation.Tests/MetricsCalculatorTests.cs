using RetrievalBenchmark.Evaluation;

namespace RetrievalBenchmark.Evaluation.Tests;

public class MetricsCalculatorTests
{
    // PrecisionAtK

    [Fact]
    public void PrecisionAtK_AllTopKAreRelevant_ReturnsOne()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "a", "b", "c" };
        Assert.Equal(1.0, MetricsCalculator.PrecisionAtK(returned, relevant, k: 3));
    }

    [Fact]
    public void PrecisionAtK_NoneOfTopKAreRelevant_ReturnsZero()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "x", "y" };
        Assert.Equal(0.0, MetricsCalculator.PrecisionAtK(returned, relevant, k: 3));
    }

    [Fact]
    public void PrecisionAtK_HalfOfTopKAreRelevant_ReturnsHalf()
    {
        var returned = new List<string> { "a", "b", "c", "d" };
        var relevant = new List<string> { "a", "c" };
        Assert.Equal(0.5, MetricsCalculator.PrecisionAtK(returned, relevant, k: 4));
    }

    [Fact]
    public void PrecisionAtK_KExceedsReturnedCount_ClampsToActualCount()
    {
        // 2 returned, both relevant — denominator should clamp to 2, not k=10
        var returned = new List<string> { "a", "b" };
        var relevant = new List<string> { "a", "b" };
        Assert.Equal(1.0, MetricsCalculator.PrecisionAtK(returned, relevant, k: 10));
    }

    [Fact]
    public void PrecisionAtK_EmptyRelevantSet_ReturnsZero()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string>();
        Assert.Equal(0.0, MetricsCalculator.PrecisionAtK(returned, relevant, k: 3));
    }

    // RecallAtK

    [Fact]
    public void RecallAtK_AllRelevantDocsInTopK_ReturnsOne()
    {
        var returned = new List<string> { "a", "b", "c", "d" };
        var relevant = new List<string> { "a", "c" };
        Assert.Equal(1.0, MetricsCalculator.RecallAtK(returned, relevant, k: 4));
    }

    [Fact]
    public void RecallAtK_NoRelevantDocsInTopK_ReturnsZero()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "x", "y" };
        Assert.Equal(0.0, MetricsCalculator.RecallAtK(returned, relevant, k: 3));
    }

    [Fact]
    public void RecallAtK_HalfOfRelevantDocsInTopK_ReturnsHalf()
    {
        // 4 relevant total, only 2 appear in top 3
        var returned = new List<string> { "a", "b", "x", "c" };
        var relevant = new List<string> { "a", "b", "c", "d" };
        Assert.Equal(0.5, MetricsCalculator.RecallAtK(returned, relevant, k: 3));
    }

    // MeanReciprocalRank

    [Fact]
    public void MeanReciprocalRank_FirstResultIsRelevant_ReturnsOne()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "a" };
        Assert.Equal(1.0, MetricsCalculator.MeanReciprocalRank(returned, relevant));
    }

    [Fact]
    public void MeanReciprocalRank_SecondResultIsRelevant_ReturnsHalf()
    {
        var returned = new List<string> { "x", "a", "b" };
        var relevant = new List<string> { "a" };
        Assert.Equal(0.5, MetricsCalculator.MeanReciprocalRank(returned, relevant));
    }

    [Fact]
    public void MeanReciprocalRank_NoRelevantResults_ReturnsZero()
    {
        var returned = new List<string> { "a", "b", "c" };
        var relevant = new List<string> { "x" };
        Assert.Equal(0.0, MetricsCalculator.MeanReciprocalRank(returned, relevant));
    }
}
