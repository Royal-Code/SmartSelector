using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Extensions;
using RoyalCode.SmartSelector.Generators.Models.Descriptors;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoSelectGenerator
{
    public const string AutoSelectAttributeFullName = "RoyalCode.SmartSelector.AutoSelectAttribute";

    private const string AutoSelectAttributeName = "AutoSelectAttribute";

    public static bool Predicate(SyntaxNode node, CancellationToken _) => node is ClassDeclarationSyntax;

    public static AutoSelectInformation Transform(
        GeneratorAttributeSyntaxContext context,
        CancellationToken __)
    {
        // classe que contém o atributo
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        // lê o atributo MapFindAttribute
        if (!classDeclaration.TryGetAttribute(AutoSelectAttributeName, out var attr))
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "The AutoSelectAttribute must be used with a class.");

            return new AutoSelectInformation(diagnostic);
        }

        // extrai o tipo from
        var syntax = (GenericNameSyntax)attr!.Name;
        var fromSyntaxType = syntax.TypeArgumentList.Arguments[0];

        var fromType = TypeDescriptor.Create(fromSyntaxType, context.SemanticModel);
        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()]);

        // obtém as propriedades da classe (classDeclaration) que podem ser atribuídas { set; }
        var properties = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) && !m.IsKind(SyntaxKind.StaticKeyword)))
            .Where(p => p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false)
            .Select(p => PropertyDescriptor.Create(p, context.SemanticModel))
            .ToList();

        // obtém as propriedades do tipo from que podem ser lidas { get; }
        var fromProperties = context.SemanticModel.GetTypeInfo(fromSyntaxType).Type
            ?.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .Where(p => p.GetMethod is not null)
            .Select(p => PropertyDescriptor.Create(p, context.SemanticModel))
            .ToList();

        if (fromProperties is null || !fromProperties.Any())
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "It was not possible to read the properties of the type from.");

            return new AutoSelectInformation(diagnostic);
        }


    }
}
