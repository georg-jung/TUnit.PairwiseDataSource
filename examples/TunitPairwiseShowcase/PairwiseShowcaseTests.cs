namespace TunitPairwiseExample;

/// <summary>
/// Demonstrates that [PairwiseDataSource] generates far fewer test cases than
/// the Cartesian product ([MatrixDataSource]) while still covering every pair
/// of parameter interactions.
/// </summary>
public class PairwiseShowcaseTests
{
    public enum Color { Red, Green, Blue }
    public enum Size { Small, Medium, Large }
    public enum Shape { Circle, Square, Triangle }

    // --- 1. Basic pairwise with explicit [Matrix] values ---
    // Cartesian product: 3 × 2 × 2 = 12 combinations
    // Pairwise: only 6 needed to cover all pairs
    [Test, PairwiseDataSource]
    public async Task StringParameters(
        [Matrix("a", "b", "c")] string first,
        [Matrix("+", "-")] string op,
        [Matrix("x", "y")] string second)
    {
        await Assert.That(first).IsNotNull();
        await Assert.That(op).IsNotNull();
        await Assert.That(second).IsNotNull();
    }

    // --- 2. Auto-generated bool and enum values ---
    // No [Matrix] attribute needed for bool/enum types!
    // Cartesian product: 2 × 3 × 3 × 3 = 54 combinations
    // Pairwise: 9
    [Test, PairwiseDataSource]
    public async Task EnumAndBoolParameters(bool enabled, Color color, Size size, Shape shape)
    {
        // Pairwise guarantees that every (enabled, color), (enabled, size),
        // (color, size), etc. pair appears at least once.
        await Assert.That(Enum.IsDefined(color)).IsTrue();
        await Assert.That(Enum.IsDefined(size)).IsTrue();
        await Assert.That(Enum.IsDefined(shape)).IsTrue();
    }

    // --- 3. Excluding specific combinations ---
    [Test, PairwiseDataSource]
    [MatrixExclusion("a", "+", "x")]
    public async Task ExcludedCombination(
        [Matrix("a", "b", "c")] string first,
        [Matrix("+", "-")] string op,
        [Matrix("x", "y")] string second)
    {
        // The combination ("a", "+", "x") will never appear
        var isForbidden = first == "a" && op == "+" && second == "x";
        await Assert.That(isForbidden).IsFalse();
    }

    // --- 4. Numeric values with [Matrix] ---
    // Cartesian: 4 × 3 × 3 = 36 combinations
    // Pairwise: 12
    [Test, PairwiseDataSource]
    public async Task NumericParameters(
        [Matrix(1, 2, 5, 10)] int retryCount,
        [Matrix(100, 500, 1000)] int timeoutMs,
        [Matrix(1.0, 2.5, 5.0)] double backoffMultiplier)
    {
        await Assert.That(retryCount).IsGreaterThan(0);
        await Assert.That(timeoutMs).IsGreaterThanOrEqualTo(100);
        await Assert.That(backoffMultiplier).IsGreaterThanOrEqualTo(1.0);
    }

    // --- 5. Mixing explicit values with auto-generated enum/bool ---
    // Cartesian: 3 × 2 × 3 = 18 combinations
    // Pairwise: 9
    [Test, PairwiseDataSource]
    public async Task MixedParameterSources(
        [Matrix("http", "https", "ftp")] string protocol,
        bool compress,
        Color color)
    {
        await Assert.That(protocol).IsNotEmpty();
    }
}
