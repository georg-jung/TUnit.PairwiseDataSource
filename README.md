# ![TUnit.PairwiseDataSource Icon](https://raw.githubusercontent.com/georg-jung/TUnit.PairwiseDataSource/master/icon.svg) TUnit.PairwiseDataSource

[![NuGet version (GeorgJung.TUnit.PairwiseDataSource)](https://img.shields.io/nuget/v/GeorgJung.TUnit.PairwiseDataSource.svg?style=flat)](https://www.nuget.org/packages/GeorgJung.TUnit.PairwiseDataSource/)
[![Build Status](https://github.com/georg-jung/TUnit.PairwiseDataSource/actions/workflows/ci.yml/badge.svg)](https://github.com/georg-jung/TUnit.PairwiseDataSource/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/georg-jung/TUnit.PairwiseDataSource/graph/badge.svg)](https://app.codecov.io/gh/georg-jung/TUnit.PairwiseDataSource)

A [TUnit](https://tunit.dev) plugin that provides pairwise (all-pairs) test case generation. Instead of testing every possible combination of parameter values (Cartesian product), pairwise testing generates a smaller set of test cases that covers every pair of parameter values at least once.

This project is closely based on [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial) by Andrew Arnott. In particular, the harder parts of the pairwise generation logic are intentionally kept very close to that codebase rather than being a fresh reimplementation. That close ancestry is deliberate: one goal of this package is to make xUnit to TUnit migration easier, including preserving the same generated pairwise cases for equivalent inputs.

## Why pairwise?

[Research](https://csrc.nist.gov/projects/automated-combinatorial-testing-for-software) shows most software defects are triggered by interactions between at most two parameters. Pairwise testing exploits this by covering all two-way interactions with significantly fewer test cases:

| Parameters | Values each | Cartesian product | Pairwise |
|------------|-------------|-------------------|----------|
| 3          | 3           | 27                | 9        |
| 4          | 3           | 81                | 9        |
| 5          | 3           | 243               | 15       |
| 3 bools    | 2           | 8                 | 4        |

## Installation

```shell
dotnet add package GerogJung.TUnit.PairwiseDataSource
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
    // Generates 6 test cases covering all pairs, instead of 12 (3×2×2) combinations
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

If you're migrating from xUnit with [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial), the mapping is straightforward.

That migration story is not accidental. This package is deliberately built on the same underlying approach, and the pairwise strategy was ported closely enough that equivalent inputs produce the same pair sets. In practice, that means moving from xUnit to TUnit does not also force you to absorb a silent change in generated test cases.

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

### Automated Migration with Code Fixers

Similar to [TUnit's automated xUnit migration](https://tunit.dev/docs/migration/xunit/#automated-migration-with-code-fixers), this package includes Roslyn analyzers and code fixers that can automatically migrate your Xunit.Combinatorial code to TUnit.PairwiseDataSource.

**What gets converted automatically:**
- `[Theory, PairwiseData]` → `[Test, PairwiseDataSource]`
- `[Theory, CombinatorialData]` → `[Test, MatrixDataSource]`
- `[CombinatorialValues(...)]` → `[Matrix(...)]`
- `[CombinatorialRange(start, count)]` → `[MatrixRange<T>(start, count)]`
- `[CombinatorialMemberData(nameof(Method))]` → `[MethodDataSource(nameof(Method))]`
- Test methods converted to `async Task`
- `using Xunit;` statements updated to TUnit usings

**How to use the code fixer:**

1. Install TUnit.PairwiseDataSource package (alongside Xunit.Combinatorial temporarily)
2. Build your project to load the analyzers
3. Run the automated code fixer:

```bash
dotnet format analyzers --severity info --diagnostics PWTUNIT002
```

4. Review the changes and remove the Xunit.Combinatorial package

**Note:** The diagnostic `PWTUNIT002` is informational-level (not an error), so it won't appear in standard build output. The code fixer handles approximately 80-90% of typical Xunit.Combinatorial usage automatically.

**What requires manual adjustment:**
- `[CombinatorialRandomData]` - No direct TUnit equivalent, needs manual conversion
- Complex parameter expressions that can't be automatically analyzed
- Custom data source implementations

## Analyzer: TUnit0049 and PWTUNIT001

TUnit's built-in analyzer emits `TUnit0049` when `[Matrix]` is used without `[MatrixDataSource]`. It doesn't know about `[PairwiseDataSource]`, so this package suppresses `TUnit0049` automatically for NuGet consumers via its shipped build props.

This package ships a **replacement analyzer** (`PWTUNIT001`) that provides equivalent protection: it errors when `[Matrix]` is used on parameters but neither `[MatrixDataSource]` nor `[PairwiseDataSource]` is present. So after suppressing `TUnit0049`, you still get a build error if you forget the data source attribute.

### Advanced package options

The package also exposes two optional MSBuild properties for consuming projects. Both are enabled by default, so you only need to set them if you want to opt out:

```xml
<PropertyGroup>
  <TUnitPairwiseDataSourceImplicitUsings>false</TUnitPairwiseDataSourceImplicitUsings>
  <TUnitPairwiseDataSourceSuppressTUnit0049>false</TUnitPairwiseDataSourceSuppressTUnit0049>
</PropertyGroup>
```

- `TUnitPairwiseDataSourceImplicitUsings`
  Adds the implicit `using TUnit.PairwiseDataSource;` for you. Set it to `false` if you prefer explicit `using` statements.
- `TUnitPairwiseDataSourceSuppressTUnit0049`
  Suppresses TUnit's `TUnit0049` analyzer diagnostic automatically. Set it to `false` if you want to keep TUnit's original diagnostic visible.

## Algorithm

The pairwise test case generation algorithm in this repository is not merely inspired by [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial). [PairwiseStrategy.cs](src/TUnit.PairwiseDataSource/PairwiseStrategy.cs) is a close port of Andrew Arnott's implementation, preserving its behavior intentionally so that migrations can keep the exact same pairwise coverage.

That implementation in Xunit.Combinatorial is itself derived from [Charlie Poole's NUnit implementation](https://github.com/nunit/nunit), originally based on Bob Jenkins' ["jenny" tool](http://burtleburtle.net/bob/math/jenny.html).

Xunit.Combinatorial is an excellent project, and this package benefits directly from that work. The goal here is not to obscure that lineage, but to bring the same proven pairwise behavior into TUnit with an API that fits naturally alongside `[MatrixDataSource]`.

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

The pairwise algorithm implementation (`PairwiseStrategy.cs`) is derived from [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial) by Andrew Arnott (itself based on Charlie Poole's NUnit implementation) and is licensed under the [Microsoft Public License (Ms-PL)](https://opensource.org/licenses/ms-pl). See [ThirdPartyNotices.txt](ThirdPartyNotices.txt) for the full license text.
