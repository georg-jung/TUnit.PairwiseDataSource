using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.PairwiseDataSource.Analyzers;

/// <summary>
/// Reports a diagnostic when <c>[Matrix]</c> is used on parameters but the method or class
/// is missing both <c>[MatrixDataSource]</c> and <c>[PairwiseDataSource]</c>.
/// </summary>
/// <remarks>
/// <para>
/// TUnit's built-in TUnit0049 diagnostic fires when <c>[Matrix]</c> is used without
/// <c>[MatrixDataSource]</c>, but it does not know about <c>[PairwiseDataSource]</c>.
/// </para>
/// <para>
/// Users of this library should suppress TUnit0049 (<c>&lt;NoWarn&gt;TUnit0049&lt;/NoWarn&gt;</c>)
/// and rely on this analyzer (PWTUNIT001) as a replacement that understands both attributes.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MatrixWithoutDataSourceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PWTUNIT001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "[MatrixDataSource] or [PairwiseDataSource] is required when using [Matrix] parameters",
        messageFormat: "[MatrixDataSourceAttribute] or [PairwiseDataSourceAttribute] is required if using [Matrix] values on your parameters",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Check if any parameter has a Matrix attribute
        bool hasMatrixParameter = method.Parameters.Any(p =>
            p.GetAttributes().Any(IsMatrixAttribute));

        if (!hasMatrixParameter)
        {
            return;
        }

        // Check if the method or its containing type has a valid data source attribute
        if (HasDataSourceAttribute(method) || HasDataSourceAttribute(method.ContainingType))
        {
            return;
        }

        // Report diagnostic on the method name
        foreach (var location in method.Locations)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }

    private static bool HasDataSourceAttribute(ISymbol? symbol)
    {
        if (symbol == null)
        {
            return false;
        }

        return symbol.GetAttributes().Any(a =>
            a.AttributeClass != null
            && (a.AttributeClass.Name == "MatrixDataSourceAttribute"
                || a.AttributeClass.Name == "PairwiseDataSourceAttribute"));
    }

    private static bool IsMatrixAttribute(AttributeData a)
    {
        if (a.AttributeClass == null)
        {
            return false;
        }

        // Match MatrixAttribute and its generic variants (MatrixRange<T>, etc.)
        string name = a.AttributeClass.Name;
        return name == "MatrixAttribute"
            || name.StartsWith("MatrixRange")
            || name.StartsWith("MatrixMethod");
    }
}
