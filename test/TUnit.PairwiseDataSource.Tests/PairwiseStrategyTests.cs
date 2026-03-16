namespace TUnit.PairwiseDataSource.Tests;

public class PairwiseStrategyTests
{
    [Test]
    public async Task SingleDimension_ReturnsAllValues()
    {
        int[][] testCases = PairwiseStrategy.GetTestCases([3]);

        await Assert.That(testCases).Count().IsEqualTo(3);

        var features = testCases.Select(tc => tc[0]).Order().ToArray();
        await Assert.That(features).IsEquivalentTo([0, 1, 2]);
    }

    [Test]
    public async Task TwoDimensions_CoversAllPairs()
    {
        int[][] testCases = PairwiseStrategy.GetTestCases([3, 2]);

        // All pairs: (0,0),(0,1),(1,0),(1,1),(2,0),(2,1) = 6 pairs = 3*2 = full product
        // For 2 dimensions, pairwise = full Cartesian product
        await Assert.That(testCases).Count().IsEqualTo(6);
    }

    [Test]
    public async Task ThreeBooleans_FewerThanCartesian()
    {
        // 3 boolean parameters: Cartesian = 2^3 = 8
        int[][] testCases = PairwiseStrategy.GetTestCases([2, 2, 2]);

        // Pairwise should produce fewer than 8 but cover all pairs
        await Assert.That(testCases.Length).IsLessThan(8);

        AssertAllPairsCovered(testCases, [2, 2, 2]);
    }

    [Test]
    public async Task ThreeDimensions_CoversAllPairs()
    {
        int[] dimensions = [3, 2, 2];
        int[][] testCases = PairwiseStrategy.GetTestCases(dimensions);

        // Cartesian product would be 3*2*2 = 12
        // Pairwise should be significantly fewer
        await Assert.That(testCases.Length).IsLessThanOrEqualTo(12);

        AssertAllPairsCovered(testCases, dimensions);
    }

    [Test]
    public async Task FourDimensions_SignificantReduction()
    {
        int[] dimensions = [3, 3, 3, 3];
        int[][] testCases = PairwiseStrategy.GetTestCases(dimensions);

        // Cartesian product would be 81
        await Assert.That(testCases.Length).IsLessThan(81);

        AssertAllPairsCovered(testCases, dimensions);
    }

    [Test]
    public async Task ManyDimensions_StillProducesResult()
    {
        int[] dimensions = [2, 3, 2, 3, 2];
        int[][] testCases = PairwiseStrategy.GetTestCases(dimensions);

        // Cartesian product would be 2*3*2*3*2 = 72
        await Assert.That(testCases.Length).IsLessThan(72);

        AssertAllPairsCovered(testCases, dimensions);
    }

    [Test]
    public async Task Deterministic_SameInputProducesSameOutput()
    {
        int[] dimensions = [3, 3, 3];

        int[][] first = PairwiseStrategy.GetTestCases(dimensions);
        int[][] second = PairwiseStrategy.GetTestCases(dimensions);

        await Assert.That(first.Length).IsEqualTo(second.Length);

        for (int i = 0; i < first.Length; i++)
        {
            await Assert.That(first[i]).IsEquivalentTo(second[i]);
        }
    }

    [Test]
    public async Task SingleValueDimensions_ProducesSingleTestCase()
    {
        int[][] testCases = PairwiseStrategy.GetTestCases([1, 1, 1]);

        await Assert.That(testCases).Count().IsEqualTo(1);
        await Assert.That(testCases[0]).IsEquivalentTo([0, 0, 0]);
    }

    private static void AssertAllPairsCovered(int[][] testCases, int[] dimensions)
    {
        // For every pair of dimensions, every combination of feature values must appear
        for (int d1 = 0; d1 < dimensions.Length; d1++)
        {
            for (int d2 = d1 + 1; d2 < dimensions.Length; d2++)
            {
                for (int f1 = 0; f1 < dimensions[d1]; f1++)
                {
                    for (int f2 = 0; f2 < dimensions[d2]; f2++)
                    {
                        bool found = testCases.Any(tc => tc[d1] == f1 && tc[d2] == f2);
                        if (!found)
                        {
                            throw new Exception(
                                $"Pair not covered: dimension {d1} feature {f1}, dimension {d2} feature {f2}");
                        }
                    }
                }
            }
        }
    }
}
