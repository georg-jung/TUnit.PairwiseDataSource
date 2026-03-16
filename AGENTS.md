# AGENTS.md

## Project overview

TUnit.PairwiseDataSource is a TUnit plugin providing pairwise (all-pairs) test case generation. It produces test cases that cover every pair of parameter values with fewer tests than a full Cartesian product.

## Build & test

```shell
dotnet build
dotnet test          # or: dotnet run --project test/TUnit.PairwiseDataSource.Tests
```

The test runner is Microsoft.Testing.Platform (configured in `global.json`), not `dotnet test`'s VSTest mode. Both `dotnet test` and `dotnet run` work.

## Architecture

- **`src/TUnit.PairwiseDataSource/`** — The NuGet library.
  - `PairwiseDataSourceAttribute.cs` — The public attribute, analogous to TUnit's built-in `MatrixDataSourceAttribute`. Inherits from `UntypedDataSourceGeneratorAttribute` and implements `IAccessesInstanceData`. Reuses TUnit's `[Matrix]` parameter attributes to collect values, then applies pairwise reduction instead of Cartesian product.
  - `PairwiseStrategy.cs` — The pairwise algorithm (internal). Ported from Xunit.Combinatorial / NUnit (Charlie Poole / Andrew Arnott). Deterministic (fixed seed PRNG).
- **`src/TUnit.PairwiseDataSource.Analyzers/`** — Roslyn analyzer (ships with the NuGet package).
  - `MatrixWithoutDataSourceAnalyzer.cs` — Emits `PWTUNIT001` when `[Matrix]` is used but neither `[MatrixDataSource]` nor `[PairwiseDataSource]` is present. Replaces TUnit's `TUnit0049` which doesn't know about `[PairwiseDataSource]`. Targets `netstandard2.0` (required for Roslyn analyzers).
  - Note: TUnit0049 is emitted by TUnit's **source generator**, not its analyzer, so a `DiagnosticSuppressor` cannot suppress it (Roslyn limitation). The `<NoWarn>TUnit0049</NoWarn>` + PWTUNIT001 approach is the only viable solution.
- **`test/TUnit.PairwiseDataSource.Tests/`** — Tests using TUnit.
  - `PairwiseStrategyTests.cs` — Unit tests for the algorithm: pair coverage, determinism, edge cases.
  - `PairwiseDataSourceAttributeTests.cs` — Integration tests exercising the attribute end-to-end.

## Code style

- Follow the `.editorconfig` in the repo root (file-scoped namespaces, 4-space indent for C#, etc.).
- Use collection expressions (`[]`) over `new List<T>()` / `new T[]{}`.
- Use primary constructors where appropriate.
- Private fields use `_camelCase` prefix.
- Target `net10.0` for the main library and tests. The analyzer project targets `netstandard2.0` (required by Roslyn).
- Central package management via `Directory.Packages.props`.
- Nerdbank.GitVersioning manages versions (`version.json`).

## Key conventions

- The `PairwiseDataSourceAttribute` should stay semantically close to `MatrixDataSourceAttribute` to make it easy to swap between full and pairwise coverage.
- The pairwise algorithm is intentionally a close port of Xunit.Combinatorial's `PairwiseStrategy` for migration compatibility and to preserve its well-tested behavior.
- The algorithm is deterministic — the same inputs always produce the same test cases.
- For ≤2 parameters, pairwise produces the same result as full Cartesian product (this is mathematically expected).
