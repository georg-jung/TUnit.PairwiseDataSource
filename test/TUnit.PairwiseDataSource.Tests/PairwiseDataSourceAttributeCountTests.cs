namespace TUnit.PairwiseDataSource.Tests;

public class PairwiseDataSourceAttributeCountTests
{
    [Test]
    public async Task SingleParam_AllValuesPresent_GeneratesThreeTests()
    {
        var method = typeof(PairwiseDataSourceAttributeTests)
            .GetMethod(nameof(PairwiseDataSourceAttributeTests.SingleParam_AllValuesPresent))!;
        var rows = await PairwiseDataSourceInProcessRunner.GetRowsAsync(method, new PairwiseDataSourceAttributeTests());

        await Assert.That(rows).Count().IsEqualTo(3);
    }

    [Test]
    public async Task TwoParams_SameAsCartesian_GeneratesFullCartesianProduct()
    {
        var method = typeof(PairwiseDataSourceAttributeTests)
            .GetMethod(nameof(PairwiseDataSourceAttributeTests.TwoParams_SameAsCartesian))!;
        var rows = await PairwiseDataSourceInProcessRunner.GetRowsAsync(method, new PairwiseDataSourceAttributeTests());

        await Assert.That(rows).Count().IsEqualTo(6);
    }

    [Test]
    public async Task NUnitDocExample_GeneratesSixTests()
    {
        var method = typeof(PairwiseDataSourceAttributeTests)
            .GetMethod(nameof(PairwiseDataSourceAttributeTests.NUnitDocExample))!;
        var rows = await PairwiseDataSourceInProcessRunner.GetRowsAsync(method, new PairwiseDataSourceAttributeTests());

        await Assert.That(rows).Count().IsEqualTo(6);
    }

    [Test]
    public async Task BoolAutoGeneration_GeneratesFourTests()
    {
        var method = typeof(PairwiseDataSourceAttributeTests)
            .GetMethod(nameof(PairwiseDataSourceAttributeTests.BoolAutoGeneration))!;
        var rows = await PairwiseDataSourceInProcessRunner.GetRowsAsync(method, new PairwiseDataSourceAttributeTests());

        await Assert.That(rows).Count().IsEqualTo(4);
    }

    [Test]
    public async Task SingleParam_WithExclusion_GeneratesRemainingValuesOnly()
    {
        var method = typeof(PairwiseDataSourceAttributeTests)
            .GetMethod(nameof(PairwiseDataSourceAttributeTests.SingleParam_WithExclusion))!;
        var rows = await PairwiseDataSourceInProcessRunner.GetRowsAsync(method, new PairwiseDataSourceAttributeTests());

        await Assert.That(rows).Count().IsEqualTo(2);
    }
}
