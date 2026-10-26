# Examples

This directory contains small example projects for `TUnit.PairwiseDataSource`.

## TunitPairwiseShowcase

`TunitPairwiseShowcase` demonstrates how `[PairwiseDataSource]` reduces the
number of generated test cases compared to `[MatrixDataSource]` while still
covering every pair of parameter values.

In this showcase:

- `[PairwiseDataSource]` executes `42` tests
- using `[MatrixDataSource]` instead, `131` tests are run

Since TUnit 1.73.0, `[PairwiseDataSource]` is built into TUnit. The project still
references the final `GeorgJung.TUnit.PairwiseDataSource` release to show that
existing tests keep compiling and now run on TUnit's built-in attribute, with
the same test cases. One extra test checks that `[PairwiseDataSource]` resolves
to `TUnit.Core`. In your own projects, just remove the package reference.

Run it with:

```shell
dotnet run

# or, from repo root:
dotnet run --project examples/TunitPairwiseShowcase
```
