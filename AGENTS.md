# AGENTS.md

## Project overview

TUnit.PairwiseDataSource is **deprecated**. It used to be a TUnit plugin providing pairwise (all-pairs) test case generation, but TUnit 1.73.0+ ships `[PairwiseDataSource]` in `TUnit.Core` (same Xunit.Combinatorial-derived algorithm and default seed). The 0.1.x implementation lives at the `v0.1.28` tag.

Version 0.2 is a final release that only hands existing `[PairwiseDataSource]` usages over to TUnit's built-in attribute. It contains no pairwise implementation.

## Build

```shell
dotnet build
dotnet pack src/TUnit.PairwiseDataSource -c Release -o ./artifacts
```

There are no unit tests anymore. CI packs the library and runs `examples/TunitPairwiseShowcase` against the freshly packed package, the same way consumers get it.

## Architecture

- **`src/TUnit.PairwiseDataSource/`** — The NuGet library.
  - `NamespaceMarker.cs` — Internal type that keeps the `TUnit.PairwiseDataSource` namespace resolvable, so existing `using TUnit.PairwiseDataSource;` directives still compile. Without a public `PairwiseDataSourceAttribute` in this namespace, `[PairwiseDataSource]` binds to `TUnit.Core.PairwiseDataSourceAttribute`.
  - `GeorgJung.TUnit.PairwiseDataSource.targets` — Shipped in `build/` and `buildTransitive/`. Emits warning `PWTUNIT002` (deprecation) and error `PWTUNIT003` when the resolved `TUnit.Core` is older than 1.73.0.
  - The package intentionally has no `TUnit.Core` dependency. Raising only `TUnit.Core` to 1.73.0 would mix TUnit package versions, which builds but discovers no tests.
- **`examples/TunitPairwiseShowcase/`** — Showcase tests. Isolated from the repo's build configuration and consumes the package from NuGet.

## Code style

- Follow the `.editorconfig` in the repo root (file-scoped namespaces, 4-space indent for C#, etc.).
- Central package management via `Directory.Packages.props`.
- Nerdbank.GitVersioning manages versions (`version.json`).
