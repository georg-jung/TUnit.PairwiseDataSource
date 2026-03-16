# TUnit.PairwiseDataSource

A [TUnit](https://tunit.dev) plugin that provides pairwise (all-pairs) test case generation. Instead of testing every possible combination of parameter values (Cartesian product), pairwise testing generates a smaller set of test cases that covers every pair of parameter values at least once.

## Why pairwise?

Research shows most software defects are triggered by interactions between at most two parameters. Pairwise testing exploits this by covering all two-way interactions with significantly fewer test cases:

| Parameters | Values each | Cartesian product | Pairwise (approx.)  |
|------------|-------------|-------------------|---------------------|
| 3          | 3           | 27                | ~9                  |
| 4          | 3           | 81                | ~9-12               |
| 5          | 3           | 243               | ~15-18              |
| 3 bools    | 2           | 8                 | 4                   |

## Installation

```shell
dotnet add package TUnit.PairwiseDataSource
```

## Usage

Use `[PairwiseDataSource]` in place of `[MatrixDataSource]` on your test methods. Parameter values are specified the same way, using `[Matrix(...)]` attributes:

```csharp
using TUnit.PairwiseDataSource;

[Test, PairwiseDataSource]
public async Task MyTest(
    [Matrix("a", "b", "c")] string first,
    [Matrix("+", "-")] string op,
    [Matrix("x", "y")] string second)
{
    // Generates ~6 test cases covering all pairs, instead of 12 (3×2×2) combinations
}
```

### Automatic value generation

Just like `[MatrixDataSource]`, boolean and enum parameters don't need explicit `[Matrix(...)]` attributes:

```csharp
public enum Priority { Low, Medium, High }

[Test, PairwiseDataSource]
public async Task TestWithAutoValues(bool enabled, Priority priority)
{
    // Generates pairwise combinations of {true,false} × {Low,Medium,High}
}
```

### Exclusions

Use `[MatrixExclusion(...)]` to exclude specific combinations:

```csharp
[Test, PairwiseDataSource]
[MatrixExclusion("a", "+", "x")]
public async Task MyTest(
    [Matrix("a", "b", "c")] string first,
    [Matrix("+", "-")] string op,
    [Matrix("x", "y")] string second)
{
    // The combination ("a", "+", "x") will never appear
}
```

## Migrating from Xunit.Combinatorial

If you're migrating from xUnit with [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial), the mapping is straightforward:

| Xunit.Combinatorial | TUnit + PairwiseDataSource |
| -------------------------------------- | ------------------------------------------- |
| `[Theory, PairwiseData]` | `[Test, PairwiseDataSource]` |
| `[Theory, CombinatorialData]` | `[Test, MatrixDataSource]` |
| `[CombinatorialValues(1, 2, 3)]` | `[Matrix(1, 2, 3)]` |
| `[CombinatorialRange(0, 5)]` | `[MatrixRange<int>(0, 5)]` |
| Bool/enum auto-generation | Bool/enum auto-generation (same) |

### Example migration

**Before (xUnit):**

```csharp
[Theory, PairwiseData]
public void MyTest(
    [CombinatorialValues("a", "b", "c")] string x,
    [CombinatorialValues("+", "-")] string y,
    [CombinatorialValues("1", "2")] string z)
{
}
```

**After (TUnit):**

```csharp
[Test, PairwiseDataSource]
public async Task MyTest(
    [Matrix("a", "b", "c")] string x,
    [Matrix("+", "-")] string y,
    [Matrix("1", "2")] string z)
{
}
```

## Analyzer: TUnit0049 and PWTUNIT001

TUnit's built-in source generator emits `TUnit0049` when `[Matrix]` is used without `[MatrixDataSource]`. It doesn't know about `[PairwiseDataSource]`, so you need to suppress it in projects that use pairwise tests:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);TUnit0049</NoWarn>
</PropertyGroup>
```

This package ships a **replacement analyzer** (`PWTUNIT001`) that provides equivalent protection: it errors when `[Matrix]` is used on parameters but neither `[MatrixDataSource]` nor `[PairwiseDataSource]` is present. So after suppressing TUnit0049, you still get a build error if you forget the data source attribute.

## Algorithm

The pairwise test case generation algorithm is based on the implementation by [Andrew Arnott](https://github.com/AArnott/Xunit.Combinatorial) (Xunit.Combinatorial), which is itself derived from [Charlie Poole's NUnit implementation](https://github.com/nunit/nunit), originally based on Bob Jenkins' ["jenny" tool](http://burtleburtle.net/bob/math/jenny.html).

## License

MIT - see [LICENSE](LICENSE) for details.

The pairwise algorithm implementation is licensed under the Ms-PL / MIT license from the original authors (Charlie Poole, Andrew Arnott).
