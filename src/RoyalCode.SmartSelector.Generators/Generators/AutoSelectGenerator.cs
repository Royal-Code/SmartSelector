using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Generators.Extensions;
using RoyalCode.SmartSelector.Generators.Models;
using RoyalCode.SmartSelector.Generators.Models.Descriptors;
using RoyalCode.SmartSelector.Generators.Models.Generators;
using RoyalCode.SmartSelector.Generators.Models.Generators.Commands;

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

        // se não existe o símbolo, não é um tipo válido
        if (classSymbol is null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),

                "The AutoSelectAttribute must be used with a class.");
            return new AutoSelectInformation(diagnostic);
        }

        // a classe com o atributo deve ter um construtor público sem parâmetros
        var constructor = classSymbol.Constructors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (constructor is null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "The class with the AutoSelectAttribute must have a public constructor without parameters.");

            return new AutoSelectInformation(diagnostic);
        }

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

        // from type deve ser uma classe
        if (fromSyntaxType is not IdentifierNameSyntax fromIdentifierName)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "Invalid type for AutoSelectAttribute, it must be a class.");

            return new AutoSelectInformation(diagnostic);
        }

        var fromType = TypeDescriptor.Create(fromSyntaxType, context.SemanticModel);
        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol);

        // obtém o símbolo do fromType
        var fromSymbol = fromType.Symbol;
        
        // se não existe o símbolo, não é um tipo válido
        if (fromSymbol is null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "Unsupported type for AutoSelectAttribute, it must be a class.");

            return new AutoSelectInformation(diagnostic);
        }

        // match das propriedades da classe com o attributo e a classe definida no TFrom.
        var matchSelection = MatchSelection.Create(modelType, fromType, context.SemanticModel);

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
        funcField.Modifiers.Static();

        // 1.2.3 adiciona o campo à classe
        partialClass.Fields.Add(funcField);
        partialClass.Usings.AddNamespaces(funcType);

        // 1.3 propriedade expression: Expression<Func<Target, Origin>> Select{Target}Expression

        // 1.3.1 cria type para Expression<Func<Target, Origin>>
        var expressionType = new TypeDescriptor($"Expression<{funcType.Name}>",
            [.. funcType.Namespaces, "System.Linq.Expressions"], null);

        // 1.3.2 cria a propriedade expression: Expression<Func<Target, Origin>> Select{Target}Expression
        var expressionProperty = new PropertyGenerator(
            expressionType, $"Select{match.TargetType.Name}Expression", true, false);

        expressionProperty.Modifiers.Public();
        expressionProperty.Modifiers.Static();

        // 1.3.3 cria a expressão lambda selecionando as propriedades
        var lambda = new SelectLambdaGenerator(match);
        expressionProperty.Value = lambda;

        // 1.3.4 adiciona a propriedade à classe
        partialClass.Properties.Add(expressionProperty);
        partialClass.Usings.AddNamespaces(expressionType);

        // 1.4 cria método static From

        // 1.4.1 cria o método
        var method = new MethodGenerator("From", match.OriginType);
        method.Modifiers.Public();
        method.Modifiers.Static();
        method.UseArrow = true;

        // 1.4.2 cria o parâmetro do método: Target target
        var paramName = match.TargetType.Name.ToLowerCamelCase();
        method.Parameters.Add(
            new ParameterGenerator(
                new ParameterDescriptor(
                    match.TargetType, paramName)));

        // 1.4.3 cria o comando de retorno
        method.Commands.Add(new CompileLambdaGenerator(
            match.TargetType.Name, paramName));

        // 1.4.4 adiciona o método à classe
        partialClass.Methods.Add(method);
        partialClass.Usings.AddNamespaces(method);

        // 1.5 Gera o código da classe
        partialClass.Generate(context);

        // 2 - Criação da classe de extenção
        var extensionClass = new ClassGenerator($"{match.OriginType.Name}_Extensions", match.OriginType.Namespaces[0]);
        extensionClass.Modifiers.Public();
        extensionClass.Modifiers.Static();

        // 2.1 Criação do método select para queryable
        
        // 2.1.1 cria tipo de retorno
        var queryType = new TypeDescriptor(
            $"IQueryable<{match.OriginType.Name}>",
            [match.OriginType.Namespaces[0], "System.Linq"], 
            null);

        // 2.1.2 cria o gerador para o método
        var queryMethod = new MethodGenerator($"Select{match.OriginType.Name}", queryType);
        queryMethod.Modifiers.Public();
        queryMethod.Modifiers.Static();

        // 2.1.3 cria o parâmetro do método
        var queryParamType = new TypeDescriptor(
            $"IQueryable<{match.TargetType.Name}>",
            [match.TargetType.Namespaces[0], "System.Linq"],
            null);

        var queryParam = new ParameterDescriptor(queryParamType, "query");
        var queryParamGenerator = new ParameterGenerator(queryParam)
        {
            ThisModifier = true
        };

        queryMethod.Parameters.Add(queryParamGenerator);

        // 2.1.4 create the method command
        var invokeSelect = new MethodInvokeGenerator(
            queryParam.Name, 
            "Select",
            $"{match.OriginType.Name}.{expressionProperty.Name}");

        queryMethod.Commands.Add(new ReturnCommand(invokeSelect));

        // 2.1.5 adiciona o método 
        extensionClass.Methods.Add(queryMethod);
        extensionClass.Usings.AddNamespaces(queryMethod);

        // 2.2 Criação do método select para enumerable
        
        // 2.2.1 cria tipo de retorno
        var enumerableType = new TypeDescriptor(
            $"IEnumerable<{match.OriginType.Name}>",
            [match.OriginType.Namespaces[0], "System.Collections.Generic"],
            null);

        // 2.2.2 cria o gerador para o método
        var enumerableMethod = new MethodGenerator($"Select{match.OriginType.Name}", enumerableType);
        enumerableMethod.Modifiers.Public();
        enumerableMethod.Modifiers.Static();

        // 2.2.3 cria o parâmetro do método
        var enumerableParamType = new TypeDescriptor(
            $"IEnumerable<{match.TargetType.Name}>",
            [match.TargetType.Namespaces[0], "System.Collections.Generic"],
            null);

        var enumerableParam = new ParameterDescriptor(enumerableParamType, "enumerable");
        var enumerableParamGenerator = new ParameterGenerator(enumerableParam)
        {
            ThisModifier = true
        };

        enumerableMethod.Parameters.Add(enumerableParamGenerator);

        // 2.2.4 create the method command
        var invokeSelectEnumerable = new MethodInvokeGenerator(
            enumerableParam.Name,
            "Select",
            $"{match.OriginType.Name}.From");

        enumerableMethod.Commands.Add(new ReturnCommand(invokeSelectEnumerable));

        // 2.2.5 adiciona o método
        extensionClass.Methods.Add(enumerableMethod);
        extensionClass.Usings.AddNamespaces(enumerableMethod);

        // 2.3 Cria método To{Origin} a partir do Target

        // 2.3.1 cria o método
        var toMethod = new MethodGenerator($"To{match.OriginType.Name}", match.OriginType);
        toMethod.Modifiers.Public();
        toMethod.Modifiers.Static();
        toMethod.UseArrow = true;

        // 2.3.2 cria o parâmetro do método: Target target
        var toParamName = match.TargetType.Name.ToLowerCamelCase();
        toMethod.Parameters.Add(
            new ParameterGenerator(new ParameterDescriptor(match.TargetType, toParamName))
            { 
                ThisModifier = true 
            });

        // 2.3.3 cria o comando que chama o método From
        var invokeFrom = new MethodInvokeGenerator(
            match.OriginType.Name,
            "From",
            toParamName);

        toMethod.Commands.Add(new Command(invokeFrom) { Idented = false, NewLine = false });

        // 2.3.4 adiciona o método à classe
        extensionClass.Methods.Add(toMethod);
        extensionClass.Usings.AddNamespaces(toMethod);

        // 2.4 Gera o código da classe de extensão
        extensionClass.Generate(context);
    }
}
