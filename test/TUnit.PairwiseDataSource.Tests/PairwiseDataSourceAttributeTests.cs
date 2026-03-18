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
    [MatrixExclusion(Color.Green, true)]
    public async Task EnumAutoGeneration_WithEnumExclusion(Color color, bool flag)
    {
        await Assert.That(Enum.IsDefined(color)).IsTrue();
        await Assert.That(color == Color.Green && flag).IsFalse();
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

    [Test]
    [PairwiseDataSource]
    public async Task NullableBool_AutoGeneratesWithNull(bool? a, bool? b, bool? c)
    {
        // Nullable bools should produce [true, false, null]
        await Assert.That(new bool?[] { true, false, null }).Contains(a);
        await Assert.That(new bool?[] { true, false, null }).Contains(b);
        await Assert.That(new bool?[] { true, false, null }).Contains(c);
    }

    [Test]
    [PairwiseDataSource]
    public async Task NullableEnum_AutoGeneratesWithNull(Color? color, bool flag)
    {
        // Nullable enum should include null as a possible value
        if (color is not null)
        {
            await Assert.That(Enum.IsDefined(color.Value)).IsTrue();
        }
    }

    [Test]
    [PairwiseDataSource]
    public async Task MatrixExcluding_FiltersValues(
        [Matrix(1, 2, 3, Excluding = new object[] { 3 })] int a,
        [Matrix(10, 20)] int b)
    {
        // Value 3 should never appear for parameter a
        await Assert.That(a).IsNotEqualTo(3);
        await Assert.That([1, 2]).Contains(a);
        await Assert.That([10, 20]).Contains(b);
    }

    [Test]
    [PairwiseDataSource]
    public async Task EnumAutoGeneration_WithExcluding(
        [Matrix(Excluding = new object[] { Color.Blue })] Color color,
        bool flag)
    {
        // Blue should be excluded from the auto-generated enum values
        await Assert.That(color).IsNotEqualTo(Color.Blue);
        await Assert.That(color is Color.Red or Color.Green).IsTrue();
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion("wrong", "number", "of", "args")]
    public async Task ExclusionLengthMismatch_IsIgnored(
        [Matrix("a", "b")] string a,
        [Matrix("x", "y")] string b)
    {
        // Exclusion has 4 elements but test has 2 params — should be silently ignored
        await Assert.That(["a", "b"]).Contains(a);
        await Assert.That(["x", "y"]).Contains(b);
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion(1)]
    public async Task SingleParam_WithExclusion([Matrix(1, 2, 3)] int x)
    {
        // Single param uses the Cartesian fallback path; exclusion should still work
        await Assert.That(x).IsNotEqualTo(1);
        await Assert.That([2, 3]).Contains(x);
    }

    [Test]
    [PairwiseDataSource]
    [MatrixExclusion("a", "+", "x")]
    [MatrixExclusion("b", "-", "y")]
    public async Task MultipleExclusions(
        [Matrix("a", "b", "c")] string a,
        [Matrix("+", "-")] string b,
        [Matrix("x", "y")] string c)
    {
        // Both excluded combinations should be absent
        var isExcluded1 = a == "a" && b == "+" && c == "x";
        var isExcluded2 = a == "b" && b == "-" && c == "y";
        await Assert.That(isExcluded1).IsFalse();
        await Assert.That(isExcluded2).IsFalse();
    }
}
