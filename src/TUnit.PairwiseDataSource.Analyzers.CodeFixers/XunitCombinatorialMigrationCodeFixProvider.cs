using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.PairwiseDataSource.Analyzers;

namespace TUnit.PairwiseDataSource.Analyzers.CodeFixers;

/// <summary>
/// Provides automated migration from Xunit.Combinatorial to TUnit.PairwiseDataSource.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XunitCombinatorialMigrationCodeFixProvider)), Shared]
public sealed class XunitCombinatorialMigrationCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(XunitCombinatorialMigrationAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Migrate to TUnit.PairwiseDataSource",
                createChangedDocument: async c => await MigrateCodeAsync(context.Document, root, c),
                equivalenceKey: "MigrateToTUnitPairwiseDataSource"),
            diagnostic);
    }

    private async Task<Document> MigrateCodeAsync(Document document, SyntaxNode? root, CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            return document;
        }

        // Apply transformations
        var rewriter = new XunitCombinatorialRewriter(semanticModel);
        var newRoot = rewriter.Visit(compilationUnit);

        if (newRoot is not CompilationUnitSyntax newCompilationUnit)
        {
            return document;
        }

        // Update usings
        newCompilationUnit = UpdateUsings(newCompilationUnit);

        return document.WithSyntaxRoot(newCompilationUnit);
    }

    private CompilationUnitSyntax UpdateUsings(CompilationUnitSyntax root)
    {
        // Remove Xunit usings
        var usingsToRemove = root.Usings
            .Where(u =>
            {
                var nameText = u.Name?.ToString();
                return nameText == "Xunit" || (nameText != null && nameText.StartsWith("Xunit."));
            })
            .ToList();

        root = root.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;

        // Add TUnit usings if not already present
        var hasCoreUsings = root!.Usings.Any(u => u.Name?.ToString() == "TUnit.Core");
        var hasAssertionsUsings = root.Usings.Any(u => u.Name?.ToString() == "TUnit.Assertions");
        var hasPairwiseUsings = root.Usings.Any(u => u.Name?.ToString() == "TUnit.PairwiseDataSource");

        var newUsings = new List<UsingDirectiveSyntax>();

        if (!hasCoreUsings)
        {
            newUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("TUnit.Core")).NormalizeWhitespace());
        }

        if (!hasAssertionsUsings)
        {
            newUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("TUnit.Assertions")).NormalizeWhitespace());
        }

        if (!hasPairwiseUsings)
        {
            newUsings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("TUnit.PairwiseDataSource")).NormalizeWhitespace());
        }

        if (newUsings.Any())
        {
            root = root.AddUsings(newUsings.ToArray());
        }

        return root;
    }

    private class XunitCombinatorialRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;

        public XunitCombinatorialRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public override SyntaxNode? VisitAttribute(AttributeSyntax node)
        {
            var name = GetAttributeName(node);

            // Convert attribute names
            var newName = name switch
            {
                "PairwiseData" or "PairwiseDataAttribute" => "PairwiseDataSource",
                "CombinatorialData" or "CombinatorialDataAttribute" => "MatrixDataSource",
                "CombinatorialValues" or "CombinatorialValuesAttribute" => "Matrix",
                _ => null
            };

            if (newName != null)
            {
                var newAttribute = node.WithName(SyntaxFactory.IdentifierName(newName));
                return base.VisitAttribute(newAttribute);
            }

            // Handle CombinatorialRange -> MatrixRange<T>
            if (name == "CombinatorialRange" || name == "CombinatorialRangeAttribute")
            {
                return ConvertCombinatorialRange(node);
            }

            // Handle CombinatorialMemberData -> MethodDataSource
            if (name == "CombinatorialMemberData" || name == "CombinatorialMemberDataAttribute")
            {
                var newAttribute = node.WithName(SyntaxFactory.IdentifierName("MethodDataSource"));
                return base.VisitAttribute(newAttribute);
            }

            // Handle CombinatorialRandomData
            if (name == "CombinatorialRandomData" || name == "CombinatorialRandomDataAttribute")
            {
                // This doesn't have a direct equivalent, add a comment
                // For now, convert to Matrix with a TODO comment
                return null; // Remove the attribute, user will need manual conversion
            }

            return base.VisitAttribute(node);
        }

        private SyntaxNode? ConvertCombinatorialRange(AttributeSyntax node)
        {
            // CombinatorialRange(start, count) -> MatrixRange<int>(start, count)
            // We need to infer the type from context, defaulting to int
            var args = node.ArgumentList?.Arguments;
            if (args == null || args.Value.Count < 2)
            {
                return base.VisitAttribute(node);
            }

            // Try to determine the parameter type from semantic model
            var parameter = node.Ancestors().OfType<ParameterSyntax>().FirstOrDefault();
            var typeArg = "int"; // default

            if (parameter != null)
            {
                var paramSymbol = _semanticModel.GetDeclaredSymbol(parameter);
                if (paramSymbol != null)
                {
                    typeArg = paramSymbol.Type.ToMinimalDisplayString(_semanticModel, parameter.SpanStart);
                }
            }

            // Create MatrixRange<T>
            var genericName = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("MatrixRange"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                        SyntaxFactory.ParseTypeName(typeArg))));

            var newAttribute = node.WithName(genericName);
            return base.VisitAttribute(newAttribute);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var newNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

            // Check if method has Theory attribute - convert to Test
            var attributes = newNode.AttributeLists.SelectMany(al => al.Attributes).ToList();
            var hasTheory = attributes.Any(a => GetAttributeName(a) is "Theory" or "TheoryAttribute");

            if (hasTheory)
            {
                // Replace Theory with Test
                var newAttributeLists = new List<AttributeListSyntax>();

                foreach (var attrList in newNode.AttributeLists)
                {
                    var newAttrs = new List<AttributeSyntax>();
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = GetAttributeName(attr);
                        if (attrName is "Theory" or "TheoryAttribute")
                        {
                            // Replace with Test
                            newAttrs.Add(attr.WithName(SyntaxFactory.IdentifierName("Test")));
                        }
                        else
                        {
                            newAttrs.Add(attr);
                        }
                    }

                    if (newAttrs.Count > 0)
                    {
                        newAttributeLists.Add(attrList.WithAttributes(
                            SyntaxFactory.SeparatedList(newAttrs)));
                    }
                }

                newNode = newNode.WithAttributeLists(
                    SyntaxFactory.List(newAttributeLists));

                // Make method async Task if not already
                if (!newNode.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    var returnType = newNode.ReturnType;
                    var isVoid = returnType is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);
                    var isTask = returnType.ToString().Contains("Task");

                    if (isVoid || !isTask)
                    {
                        newNode = newNode
                            .WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space))
                            .WithModifiers(newNode.Modifiers.Add(
                                SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                    }
                }
            }

            return newNode;
        }

        private static string GetAttributeName(AttributeSyntax attribute)
        {
            return attribute.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.Text,
                SimpleNameSyntax simpleName => simpleName.Identifier.Text,
                _ => ""
            };
        }
    }
}
