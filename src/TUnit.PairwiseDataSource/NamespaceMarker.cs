namespace TUnit.PairwiseDataSource;

/// <summary>
/// Keeps the <c>TUnit.PairwiseDataSource</c> namespace resolvable, so existing
/// <c>using TUnit.PairwiseDataSource;</c> directives still compile. <c>[PairwiseDataSource]</c>
/// itself now binds to TUnit's built-in <c>TUnit.Core.PairwiseDataSourceAttribute</c> (TUnit 1.73.0+).
/// </summary>
internal static class NamespaceMarker
{
}
