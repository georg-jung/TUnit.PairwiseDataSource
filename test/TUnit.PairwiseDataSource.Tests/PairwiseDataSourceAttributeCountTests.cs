namespace TUnit.PairwiseDataSource.Tests;

public class PairwiseDataSourceAttributeCountTests
{
    [Test]
    public async Task SingleParam_AllValuesPresent_GeneratesThreeTests()
    {
        var result = await TrxTestProjectRunner.RunMainTestProject(
            "/*/*/PairwiseDataSourceAttributeTests/SingleParam_AllValuesPresent*");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Total).IsEqualTo(3);
        await Assert.That(result.Passed).IsEqualTo(3);
        await Assert.That(result.Failed).IsEqualTo(0);
    }

    [Test]
    public async Task TwoParams_SameAsCartesian_GeneratesFullCartesianProduct()
    {
        var result = await TrxTestProjectRunner.RunMainTestProject(
            "/*/*/PairwiseDataSourceAttributeTests/TwoParams_SameAsCartesian*");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Total).IsEqualTo(6);
        await Assert.That(result.Passed).IsEqualTo(6);
        await Assert.That(result.Failed).IsEqualTo(0);
    }

    [Test]
    public async Task NUnitDocExample_GeneratesSixTests()
    {
        var result = await TrxTestProjectRunner.RunMainTestProject(
            "/*/*/PairwiseDataSourceAttributeTests/NUnitDocExample*");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Total).IsEqualTo(6);
        await Assert.That(result.Passed).IsEqualTo(6);
        await Assert.That(result.Failed).IsEqualTo(0);
    }

    [Test]
    public async Task BoolAutoGeneration_GeneratesFourTests()
    {
        var result = await TrxTestProjectRunner.RunMainTestProject(
            "/*/*/PairwiseDataSourceAttributeTests/BoolAutoGeneration*");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Total).IsEqualTo(4);
        await Assert.That(result.Passed).IsEqualTo(4);
        await Assert.That(result.Failed).IsEqualTo(0);
    }

    [Test]
    public async Task SingleParam_WithExclusion_GeneratesRemainingValuesOnly()
    {
        var result = await TrxTestProjectRunner.RunMainTestProject(
            "/*/*/PairwiseDataSourceAttributeTests/SingleParam_WithExclusion*");

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Total).IsEqualTo(2);
        await Assert.That(result.Passed).IsEqualTo(2);
        await Assert.That(result.Failed).IsEqualTo(0);
    }
}
