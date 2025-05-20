using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Generators.Extensions;
using RoyalCode.SmartSelector.Generators.Models;
using RoyalCode.SmartSelector.Generators.Models.Descriptors;
using RoyalCode.SmartSelector.Generators.Models.Generators;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoSelectGenerator
{
    public const string AutoSelectAttributeFullName = "RoyalCode.SmartSelector.AutoSelectAttribute`1";

    private const string AutoSelectAttributeName = "AutoSelect";

    public static bool Predicate(SyntaxNode node, CancellationToken _)
    {
        var accept = node is ClassDeclarationSyntax;
        return accept;
    }

    public static AutoSelectInformation Transform(
        GeneratorAttributeSyntaxContext context,
        CancellationToken __)
    {
        // classe que contém o atributo
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        // extrai o symbol do classDeclaration
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        // lê o atributo AutoSelectAttribute
        if (!classDeclaration.TryGetAttribute(AutoSelectAttributeName, out var attr))
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "The AutoSelectAttribute must be used with a class.");

            return new AutoSelectInformation(diagnostic);
        }

        // valida a classe, que deve ser partial
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "The AutoSelectAttribute must be used with a partial class.");

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
        var matchSelection = MatchSelection.Create(origin, target, context.SemanticModel);

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

        // se há propriedades que não podem ser atribuídas, exibe o(s) erro(s)
        if (matchSelection.HasNotAssignableProperties(out var notAssignableProperties))
        {
            List<Diagnostic> diagnostics = [];
            foreach (var property in notAssignableProperties)
            {
                // obtém o syntax token da propriedade a partir do classDeclaration
                var propertySyntax = classDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.Text == property.Origin.Name);

                var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.PropertyNotCompatible,
                    location: propertySyntax?.Identifier.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
                    property.Origin.Name,
                    property.Origin.Type.Name,
                    property.Target!.PropertyType.Name,
                    property.Target!.PropertyType.Type.Name);

                diagnostics.Add(diagnostic);
            }

            return new AutoSelectInformation(diagnostics.ToArray());
        }

        return new AutoSelectInformation(matchSelection);
    }

    public static void Generate(MatchSelection match, SourceProductionContext context)
    {
        // gera o código de seleção automática

        // 1 - criação da classe partial
        var partialClass = new ClassGenerator(match.OriginType.Name, match.OriginType.Namespaces[0]);

        // 1.1 modificadores
        if (match.OriginType.Symbol?.DeclaredAccessibility == Accessibility.Public)
        {
            partialClass.Modifiers.Public();
        }
        if (match.OriginType.Symbol?.DeclaredAccessibility == Accessibility.Internal)
        {
            partialClass.Modifiers.Internal();
        }
        if (match.OriginType.Symbol?.DeclaredAccessibility == Accessibility.Protected)
        {
            partialClass.Modifiers.Protected();
        }
        if (match.OriginType.Symbol?.DeclaredAccessibility == Accessibility.Private)
        {
            partialClass.Modifiers.Private();
        }

        partialClass.Modifiers.Partial();

        // 1.2 campo privado para a func

        // 1.2.1 cria type para Func<Target, Origin>
        var funcType = new TypeDescriptor($"Func<{match.TargetType.Name}, {match.OriginType.Name}>",
            [match.TargetType.Namespaces[0], match.OriginType.Namespaces[0], "System"], null);

        // 1.2.2 cria o campo privado para a func "select{Target}Func"
        var funcField = new FieldGenerator(funcType, $"select{match.TargetType.Name}Func", false);
        funcField.Modifiers.Private();

        // 1.2.3 adiciona o campo à classe
        partialClass.Fields.Add(funcField);
    }
}
