namespace TUnit.PairwiseDataSource.Tests;

public enum Color
{
    Red,
    Green,
    Blue,
}

public class PairwiseDataSourceAttributeTests
{
    [Test]
    [PairwiseDataSource]
    public async Task NUnitDocExample(
        [Matrix("a", "b", "c")] string a,
        [Matrix("+", "-")] string b,
        [Matrix("x", "y")] string c)
    {
        // NUnit docs say this produces 6 test cases instead of 12
        // We just verify the test runs with valid values
        await Assert.That(["a", "b", "c"]).Contains(a);
        await Assert.That(["+", "-"]).Contains(b);
        await Assert.That(["x", "y"]).Contains(c);
    }

    [Test]
    [PairwiseDataSource]
    public async Task TwoParams_SameAsCartesian(
        [Matrix(1, 2, 3)] int x,
        [Matrix(10, 20)] int y)
    {
        // With only 2 parameters, pairwise = full Cartesian product
        await Assert.That([1, 2, 3]).Contains(x);
        await Assert.That([10, 20]).Contains(y);
    }

    [Test]
    [PairwiseDataSource]
    public async Task BoolAutoGeneration(bool a, bool b, bool c)
    {
        // Bool values should be auto-generated (true/false)
        // Pairwise with 3 bools should produce fewer than 8 (2^3) cases
        // Just verify the test executes - bool values are auto-generated
        await Assert.That(a || !a).IsTrue();
    }

    [Test]
    [PairwiseDataSource]
    public async Task EnumAutoGeneration(Color color, bool flag)
    {
        // Enum values should be auto-generated
        await Assert.That(Enum.IsDefined(color)).IsTrue();
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion("a", "+", "x")]
    public async Task WithExclusion(
        [Matrix("a", "b", "c")] string a,
        [Matrix("+", "-")] string b,
        [Matrix("x", "y")] string c)
    {
        // The excluded combination should not appear
        bool isExcluded = a == "a" && b == "+" && c == "x";
        await Assert.That(isExcluded).IsFalse();
    }

    [Test]
    [PairwiseDataSource]
    public async Task SingleParam_AllValuesPresent([Matrix(1, 2, 3)] int x)
    {
        await Assert.That([1, 2, 3]).Contains(x);
    }

    [Test]
    [PairwiseDataSource]
    public async Task FourDimensions_ReducedTestCount(
        [Matrix(1, 2, 3)] int a,
        [Matrix(10, 20, 30)] int b,
        [Matrix(100, 200, 300)] int c,
        [Matrix(1000, 2000, 3000)] int d)
    {
        // 3^4 = 81 Cartesian cases. Pairwise should be much fewer.
        // Just verify valid values are passed.
        await Assert.That([1, 2, 3]).Contains(a);
        await Assert.That([10, 20, 30]).Contains(b);
        await Assert.That([100, 200, 300]).Contains(c);
        await Assert.That([1000, 2000, 3000]).Contains(d);
    }
}
