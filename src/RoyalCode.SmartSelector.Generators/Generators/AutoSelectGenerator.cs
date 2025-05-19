using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Extensions;
using RoyalCode.SmartSelector.Generators.Models;
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

        // extrai o symbol do classDeclaration
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

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
        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol);

        // obtém as propriedades da classe que podem ser atribuídas { set; }
        var properties = modelType.CreateProperties(p => p.SetMethod is not null);

        // obtém as propriedades do tipo from que podem ser lidas { get; }
        var fromProperties = fromType.CreateProperties(p => p.GetMethod is not null);

        if (fromProperties is null || !fromProperties.Any())
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "It was not possible to read the properties of the type from.");

            return new AutoSelectInformation(diagnostic);
        }

        // entrada para realizar o match
        var origin = new MatchTypeInfo(modelType, properties);
        var target = new MatchTypeInfo(fromType, fromProperties);
        
        // match das propriedades da classe com o attributo com a classe from.
        var matchSelection = MatchSelection.Create(origin, target);

        // se houve propriedades que não foram encontradas, exibe o(s) erro(s)
        if (matchSelection.HasMissingProperties(out var missingProperties))
        {
            List<Diagnostic> diagnostics = [];
            foreach (var property in missingProperties)
            {
                // obtém o syntax token da propriedade a partir do classDeclaration
                var propertySyntax = classDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.Text == property.Name);

                var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.PropertyNotMatch,
                    location: propertySyntax?.Identifier.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
                    property.Name);

                diagnostics.Add(diagnostic);
            }

            return new AutoSelectInformation(diagnostics.ToArray());
        }


    }
}
