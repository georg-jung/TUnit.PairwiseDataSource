# ![TUnit.PairwiseDataSource Icon](https://raw.githubusercontent.com/georg-jung/TUnit.PairwiseDataSource/master/icon.svg) TUnit.PairwiseDataSource

[![NuGet version (GeorgJung.TUnit.PairwiseDataSource)](https://img.shields.io/nuget/v/GeorgJung.TUnit.PairwiseDataSource.svg?style=flat)](https://www.nuget.org/packages/GeorgJung.TUnit.PairwiseDataSource/)
[![Build Status](https://github.com/georg-jung/TUnit.PairwiseDataSource/actions/workflows/ci.yml/badge.svg)](https://github.com/georg-jung/TUnit.PairwiseDataSource/actions/workflows/ci.yml)

> **Deprecated:** [TUnit](https://tunit.dev) 1.73.0 and later include `[PairwiseDataSource]` built in. It uses the same pairwise algorithm from [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial), contributed to TUnit by Andrew Arnott. This package is no longer maintained. Please [migrate to the built-in attribute](#migrating-to-tunits-built-in-pairwisedatasource).

This package added pairwise (all-pairs) test case generation to TUnit before TUnit had it: a `[PairwiseDataSource]` attribute that works like `[MatrixDataSource]`, but covers every pair of parameter values instead of every combination. See TUnit's [pairwise tests documentation](https://tunit.dev/docs/writing-tests/pairwise-tests) for the built-in version.

## Migrating to TUnit's built-in `[PairwiseDataSource]`

1. Update TUnit to 1.73.0 or later.
2. Remove the `GeorgJung.TUnit.PairwiseDataSource` package reference.
3. Delete any `using TUnit.PairwiseDataSource;` directives. If you set them, also remove the `TUnitPairwiseDataSourceImplicitUsings` and `TUnitPairwiseDataSourceSuppressTUnit0049` MSBuild properties and any `PWTUNIT001` suppressions.

Your tests don't need any other changes: `[PairwiseDataSource]`, `[Matrix(...)]`, `[MatrixRange<T>(...)]`, `[MatrixMethod<T>(...)]`, `[MatrixExclusion(...)]` and the automatic `bool`/enum values all work the same with TUnit's attribute. TUnit's own `TUnit0049` analyzer now recognizes `[PairwiseDataSource]`, so this package's `PWTUNIT001` replacement is no longer needed.

### What changes

- **Without `[MatrixExclusion]`:** TUnit generates exactly the same test cases as this package. Both use the same algorithm and default seed.
- **With `[MatrixExclusion]`:** TUnit applies exclusions while it generates the test cases, instead of filtering them out afterwards as this package did. Every pair that can still occur is now covered, so these tests may get different (and sometimes more) test cases than before.
- TUnit adds a `Seed` property (`[PairwiseDataSource(Seed = 42)]`) to pick a different, equally valid set of test cases.

### Seeing error CS0104?

```
error CS0104: 'PairwiseDataSource' is an ambiguous reference between 'TUnit.Core.PairwiseDataSourceAttribute' and 'TUnit.PairwiseDataSource.PairwiseDataSourceAttribute'
```

You are using version 0.1.x of this package with TUnit 1.73.0 or later. Follow the [migration steps](#migrating-to-tunits-built-in-pairwisedatasource) above, or update this package to 0.2.

### The final 0.2 release

Version 0.2 is a final release that only bridges the gap until you remove the package. It contains no pairwise implementation of its own. With it, your existing code compiles against TUnit 1.73.0+ unchanged, including `using TUnit.PairwiseDataSource;` directives, and `[PairwiseDataSource]` already uses TUnit's built-in attribute.

- It reports warning `PWTUNIT002` as a reminder to remove the package. To silence it, add `<NoWarn>$(NoWarn);PWTUNIT002</NoWarn>` to your project.
- It fails with error `PWTUNIT003` if your TUnit version is older than 1.73.0. If you have to stay on an older TUnit, keep using version 0.1.x.

## Migrating from Xunit.Combinatorial

This package was built to make migrating from xUnit with [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial) easy, generating the same pairwise test cases for equivalent inputs. TUnit's built-in attribute keeps that promise:

| Xunit.Combinatorial | TUnit |
| -------------------------------------- | ------------------------------------------- |
| `[Theory, PairwiseData]` | `[Test, PairwiseDataSource]` |
| `[Theory, CombinatorialData]` | `[Test, MatrixDataSource]` |
| `[CombinatorialValues(1, 2, 3)]` | `[Matrix(1, 2, 3)]` |
| `[CombinatorialRange(0, 5)]` | `[MatrixRange<int>(0, 5)]` |
| Bool/enum auto-generation | Bool/enum auto-generation (same) |

## History

The pairwise algorithm in versions 0.1.x was a close port of Andrew Arnott's implementation in [Xunit.Combinatorial](https://github.com/AArnott/Xunit.Combinatorial). That implementation is itself derived from [Charlie Poole's NUnit implementation](https://github.com/nunit/nunit), originally based on Bob Jenkins' ["jenny" tool](http://burtleburtle.net/bob/math/jenny.html). The 0.1.x source code is available at the [`v0.1.28` tag](https://github.com/georg-jung/TUnit.PairwiseDataSource/tree/v0.1.28).

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

Versions 0.1.x also contained the pairwise algorithm derived from Xunit.Combinatorial, which is licensed under the [Microsoft Public License (Ms-PL)](https://opensource.org/licenses/ms-pl). Those versions include its license text in `ThirdPartyNotices.txt`.
