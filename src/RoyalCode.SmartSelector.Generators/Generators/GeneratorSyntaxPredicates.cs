using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class GeneratorSyntaxPredicates
{
    internal static bool IsClass(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax;
}
