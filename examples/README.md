# Examples

This directory contains small example projects for `TUnit.PairwiseDataSource`.

## TunitPairwiseShowcase

`TunitPairwiseShowcase` demonstrates how `[PairwiseDataSource]` reduces the
number of generated test cases compared to `[MatrixDataSource]` while still
covering every pair of parameter values.

In this showcase:

- `[PairwiseDataSource]` executes `42` tests
- using `[MatrixDataSource]` instead, `131` tests are run

Run it with:

```shell
dotnet run

# or, from repo root:
dotnet run --project examples/TunitPairwiseShowcase
```
