using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.PairwiseDataSource.Analyzers;

/// <summary>
/// Detects Xunit.Combinatorial usage and suggests migration to TUnit.PairwiseDataSource.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XunitCombinatorialMigrationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PWTUNIT002";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Migrate from Xunit.Combinatorial to TUnit.PairwiseDataSource",
        messageFormat: "Consider migrating from Xunit.Combinatorial to TUnit.PairwiseDataSource",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Xunit.Combinatorial code can be automatically migrated to TUnit.PairwiseDataSource.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CompilationUnit);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return;
        }

        var classDeclarationSyntaxes = compilationUnitSyntax
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol is null)
            {
                continue;
            }

            // Check if any method or parameter uses Xunit.Combinatorial attributes
            foreach (var methodSymbol in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (HasXunitCombinatorialAttributes(context, methodSymbol))
                {
                    Flag(context);
                    return;
                }
            }

            // Check for using directives
            var usingDirectiveSyntaxes = classDeclarationSyntax
                .SyntaxTree
                .GetCompilationUnitRoot()
                .Usings;

            foreach (var usingDirectiveSyntax in usingDirectiveSyntaxes)
            {
                if (usingDirectiveSyntax.Name is QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.Text: "Xunit" } }
                    or IdentifierNameSyntax { Identifier.Text: "Xunit" })
                {
                    var nameText = usingDirectiveSyntax.Name.ToString();
                    if (nameText == "Xunit" || nameText.StartsWith("Xunit."))
                    {
                        // Do additional check to see if Combinatorial is actually used
                        if (HasCombinatorialUsage(compilationUnitSyntax))
                        {
                            Flag(context);
                            return;
                        }
                    }
                }
            }
        }

        // Check for global usings at the compilation unit level
        foreach (var usingDirective in compilationUnitSyntax.Usings)
        {
            if (!usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            {
                continue;
            }

            var nameText = usingDirective.Name?.ToString();
            if (nameText == "Xunit" || (nameText != null && nameText.StartsWith("Xunit.")))
            {
                if (HasCombinatorialUsage(compilationUnitSyntax))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.GetLocation()));
                    return;
                }
            }
        }
    }

    private static bool HasXunitCombinatorialAttributes(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol)
    {
        // Check method attributes
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            var @namespace = attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString();
            var name = attributeData.AttributeClass?.Name;

            if (@namespace == "Xunit" && (name == "PairwiseDataAttribute" || name == "CombinatorialDataAttribute"))
            {
                return true;
            }
        }

        // Check parameter attributes
        foreach (var parameter in methodSymbol.Parameters)
        {
            foreach (var attributeData in parameter.GetAttributes())
            {
                var @namespace = attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString();
                var name = attributeData.AttributeClass?.Name;

                if (@namespace == "Xunit" && (
                    name == "CombinatorialValuesAttribute" ||
                    name == "CombinatorialRangeAttribute" ||
                    name == "CombinatorialMemberDataAttribute" ||
                    name == "CombinatorialRandomDataAttribute"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasCombinatorialUsage(CompilationUnitSyntax root)
    {
        // Look for PairwiseData, CombinatorialData, CombinatorialValues, etc. in attributes
        var attributes = root.DescendantNodes().OfType<AttributeSyntax>();
        foreach (var attr in attributes)
        {
            var name = GetAttributeName(attr);
            if (name.StartsWith("PairwiseData") ||
                name.StartsWith("CombinatorialData") ||
                name.StartsWith("CombinatorialValues") ||
                name.StartsWith("CombinatorialRange") ||
                name.StartsWith("CombinatorialMemberData") ||
                name.StartsWith("CombinatorialRandomData"))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetAttributeName(AttributeSyntax attribute)
    {
        return attribute.Name switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.Text,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.Text,
            _ => ""
        };
    }

    private static void Flag(SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
    }
}
