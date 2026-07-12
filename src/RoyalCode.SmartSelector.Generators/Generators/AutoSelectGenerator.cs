using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Generators.Models;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoSelectGenerator
{
    private static readonly SymbolDisplayFormat TypeNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public const string AutoSelectAttributeFullName = "RoyalCode.SmartSelector.AutoSelectAttribute`1";
    public const string AutoPropertiesAttributeFullName = "RoyalCode.SmartSelector.AutoPropertiesAttribute";

    public static AutoSelectInformation Transform(
        GeneratorAttributeSyntaxContext context,
        CancellationToken __)
    {
        // classe que contém o atributo
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        // Extrai o símbolo da declaração da classe.
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

        // Lê o atributo semanticamente. O argumento pode ter qualquer forma de nome de tipo válida
        // em C# (qualificado, alias global, tipo aninhado ou tipo genérico).
        var autoSelectAttribute = context.Attributes.FirstOrDefault();
        var fromSymbol = autoSelectAttribute?.AttributeClass?.TypeArguments.FirstOrDefault();
        if (fromSymbol is null)
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

        if (classSymbol.Arity > 0)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.GenericDestinationTypeNotSupported,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            return new AutoSelectInformation(diagnostic);
        }

        if (classSymbol.ContainingType is not null)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.NestedDestinationTypeNotSupported,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            return new AutoSelectInformation(diagnostic);
        }

        if (classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.GlobalNamespaceNotSupported,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            return new AutoSelectInformation(diagnostic);
        }

        // O tipo de origem precisa ser uma classe.
        if (fromSymbol.TypeKind != TypeKind.Class)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoSelectType,
                location: classDeclaration.Identifier.GetLocation(),
                "Invalid type for AutoSelectAttribute, it must be a class.");

            return new AutoSelectInformation(diagnostic);
        }

        var symbolType = TypeDescriptor.Create(fromSymbol);
        var fromType = new TypeDescriptor(
            fromSymbol.ToDisplayString(TypeNameFormat),
            symbolType.Namespaces,
            fromSymbol);
        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol);

        // Verifica semanticamente os atributos AutoProperties aplicados à classe.
        AutoPropertiesInformation? propertiesInfo = null;
        var attributes = classSymbol.GetAttributes();
        var typedAutoProperties = attributes.FirstOrDefault(attribute =>
            attribute.AttributeClass?.MetadataName == "AutoPropertiesAttribute`1" &&
            attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");
        if (typedAutoProperties is not null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoProperty,
                location: classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);

            return new AutoSelectInformation(diagnostic);
        }

        var autoProperties = attributes.FirstOrDefault(attribute =>
            attribute.AttributeClass?.MetadataName == "AutoPropertiesAttribute" &&
            attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");
        if (autoProperties is not null)
        {
            propertiesInfo = AutoPropertiesGenerator.CreateInformation(modelType, fromType, autoProperties);
        }

        // Corresponde as propriedades da classe com o atributo e a classe definida em TFrom.
        var matchSelection = MatchSelection.Create(
            modelType,
            fromType,
            context.SemanticModel,
            AutoPropertiesGenerator.MatchOptions);

        // Diagnósticos de AutoDetails prevalecem sobre as falhas de correspondência das mesmas propriedades.
        var autoDetailsDiagnostics = GetAutoDetailsDiagnostics(propertiesInfo, out var autoDetailsFailedProperties);

        // se houve propriedades que não foram encontradas, exibe o(s) erro(s)
        if (matchSelection.HasMissingProperties(out var missingProperties))
        {
            List<Diagnostic> diagnostics = [.. autoDetailsDiagnostics];
            foreach (var property in missingProperties)
            {
                if (autoDetailsFailedProperties.Contains(property.Name))
                    continue;

                // Obtém o token sintático da propriedade na declaração da classe.
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
            List<Diagnostic> diagnostics = [.. autoDetailsDiagnostics];
            foreach (var property in notAssignableProperties)
            {
                if (autoDetailsFailedProperties.Contains(property.Origin.Name))
                    continue;

                // Obtém o token sintático da propriedade na declaração da classe.
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

        var flatteningDiagnostics = CreateFlatteningDiagnostics(classDeclaration, fromType);
        return new AutoSelectInformation(matchSelection, propertiesInfo, flatteningDiagnostics);
    }

    private static Diagnostic[] GetAutoDetailsDiagnostics(
        AutoPropertiesInformation? propertiesInfo,
        out HashSet<string> failedProperties)
    {
        failedProperties = new HashSet<string>(StringComparer.Ordinal);
        if (propertiesInfo is null)
            return [];

        List<Diagnostic> diagnostics = [];
        foreach (var autoDetail in propertiesInfo.AutoDetails)
        {
            if (autoDetail.Diagnostics is not { Length: > 0 } autoDetailDiagnostics)
                continue;

            diagnostics.AddRange(autoDetailDiagnostics);
            if (autoDetail.PropertyName is not null)
                failedProperties.Add(autoDetail.PropertyName);
        }

        return [.. diagnostics];
    }

    private static Diagnostic[] CreateFlatteningDiagnostics(
        ClassDeclarationSyntax classDeclaration,
        TypeDescriptor fromType)
    {
        List<Diagnostic> diagnostics = [];
        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (CountPropertyPaths(fromType, property.Identifier.Text) < 2)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                AnalyzerDiagnostics.AmbiguousFlattening,
                property.Identifier.GetLocation(),
                property.Identifier.Text));
        }

        return diagnostics.ToArray();
    }

    private static int CountPropertyPaths(TypeDescriptor type, string remainingName)
    {
        var properties = type.CreateProperties(property => property.GetMethod is not null);
        if (properties.Any(property => property.Name == remainingName))
        {
            return 1;
        }

        var count = 0;
        foreach (var property in properties)
        {
            if (property.Name.Length >= remainingName.Length ||
                !remainingName.StartsWith(property.Name, StringComparison.Ordinal))
            {
                continue;
            }

            count += CountPropertyPaths(
                property.Type,
                remainingName.Substring(property.Name.Length));
            if (count > 1)
            {
                return count;
            }
        }

        return count;
    }

    public static void Generate(MatchSelection match, SourceProductionContext context)
    {
        // gera o código de seleção automática

        // TypeDescriptor.Name representa a sintaxe completa do tipo e pode conter pontos ou
        // argumentos genéricos. Nomes de membros e parâmetros precisam usar apenas o identificador.
        var targetTypeIdentifier = match.TargetType.Symbol?.Name ?? match.TargetType.Name;

        // 1 - criação da classe partial
        var partialClass = new ClassGenerator(match.OriginType.Name, match.OriginType.Namespaces[0]);
        GeneratedSourceConventions.ApplyRequiredNamespaces(partialClass);
        partialClass.FileName = GeneratedSourceConventions.FileName(
            match.OriginType,
            match.OriginType.Name,
            "AutoSelect");

        // 1.1 modificadores
        GeneratedSourceConventions.ApplyDeclaredAccessibility(
            partialClass,
            match.OriginType.Symbol);

        partialClass.Modifiers.Partial();

        // 1.2 campo privado para a func

        // 1.2.1 cria o tipo Func<Target, Origin>
        var funcType = new TypeDescriptor($"Func<{match.TargetType.Name}, {match.OriginType.Name}>",
            [match.TargetType.Namespaces[0], match.OriginType.Namespaces[0], "System"], null);

        // 1.2.2 cria o campo privado nullable para a função "select{Target}Func" (DF6)
        var funcFieldType = new TypeDescriptor($"{funcType.Name}?", funcType.Namespaces, null);
        var funcField = new AnnotatedFieldGenerator(
            funcFieldType,
            $"select{targetTypeIdentifier}Func",
            [GeneratedSourceConventions.GeneratedCodeAttributeLine]);
        funcField.Modifiers.Private();
        funcField.Modifiers.Static();

        // 1.2.3 adiciona o campo à classe
        partialClass.Fields.Add(funcField);

        // 1.3 propriedade expression: Expression<Func<Target, Origin>> Select{Target}Expression

        // 1.3.1 cria o tipo Expression<Func<Target, Origin>>
        var expressionType = new TypeDescriptor($"Expression<{funcType.Name}>",
            [.. funcType.Namespaces, "System.Linq.Expressions"], null);

        // 1.3.2 cria a propriedade expression: Expression<Func<Target, Origin>> Select{Target}Expression
        var expressionProperty = new AnnotatedPropertyGenerator(
            expressionType,
            $"Select{targetTypeIdentifier}Expression",
            [
                $"/// <summary>Projection expression that creates a new <see cref=\"{match.OriginType.Name}\"/> from a <see cref=\"{match.TargetType.Name}\"/>.</summary>",
                GeneratedSourceConventions.GeneratedCodeAttributeLine,
            ],
            true, false);

        expressionProperty.Modifiers.Public();
        expressionProperty.Modifiers.Static();
        if (HasAccessibleBaseMember(match.OriginType.Symbol, expressionProperty.Name))
        {
            expressionProperty.Modifiers.New();
        }

        // 1.3.3 cria a expressão lambda selecionando as propriedades
        var lambda = new SelectLambdaGenerator(match);
        expressionProperty.Value = lambda;

        // 1.3.4 adiciona a propriedade à classe
        partialClass.Properties.Add(expressionProperty);

        // 1.4 cria método static From

        // 1.4.1 cria o método
        var method = new MethodGenerator("From", match.OriginType);
        var paramName = targetTypeIdentifier.ToLowerCamelCase();
        method.Attributes.Add(new RawLinesGeneratorNode(
            $"/// <summary>Creates a new <see cref=\"{match.OriginType.Name}\"/> projected from a <see cref=\"{match.TargetType.Name}\"/> instance.</summary>",
            $"/// <param name=\"{paramName}\">The source instance to project.</param>",
            $"/// <returns>A new <see cref=\"{match.OriginType.Name}\"/> instance.</returns>",
            GeneratedSourceConventions.GeneratedCodeAttributeLine));
        method.Modifiers.Public();
        method.Modifiers.Static();
        if (HasAccessibleBaseMember(match.OriginType.Symbol, method.Name))
        {
            method.Modifiers.New();
        }
        method.UseArrow = true;

        // 1.4.2 cria o parâmetro do método: Target target
        method.Parameters.Add(
            new ParameterGenerator(
                new ParameterDescriptor(
                    match.TargetType, paramName)));

        // 1.4.3 cria o comando de retorno
        method.Commands.Add(new CompileLambdaGenerator(
            targetTypeIdentifier, paramName));

        // 1.4.4 adiciona o método à classe
        partialClass.Methods.Add(method);

        // 1.5 Gera o código da classe
        partialClass.Generate(context);

        // 2 - Criação da classe de extensão
        var extensionClass = new ClassGenerator($"{match.OriginType.Name}_Extensions", match.OriginType.Namespaces[0]);
        GeneratedSourceConventions.ApplyRequiredNamespaces(extensionClass);
        extensionClass.FileName = GeneratedSourceConventions.FileName(
            match.OriginType,
            match.OriginType.Name,
            "Extensions");

        // classe sem nenhuma declaração do usuário: docs e [GeneratedCode] no tipo (DF11)
        extensionClass.Attributes.Add(new RawLinesGeneratorNode(
            $"/// <summary>Generated extension methods to project <see cref=\"{match.TargetType.Name}\"/> instances into <see cref=\"{match.OriginType.Name}\"/> instances.</summary>",
            GeneratedSourceConventions.GeneratedCodeAttributeLine));

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
        queryMethod.Attributes.Add(new RawLinesGeneratorNode(
            $"/// <summary>Projects the <see cref=\"{match.TargetType.Name}\"/> query into <see cref=\"{match.OriginType.Name}\"/>.</summary>",
            "/// <param name=\"query\">The source query.</param>",
            $"/// <returns>An <see cref=\"IQueryable{{T}}\"/> of <see cref=\"{match.OriginType.Name}\"/>.</returns>"));
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

        // 2.1.4 cria o comando do método
        var invokeSelect = new MethodInvokeGenerator(
            queryParam.Name, 
            "Select",
            $"{match.OriginType.Name}.{expressionProperty.Name}");

        queryMethod.Commands.Add(new ReturnCommand(invokeSelect));

        // 2.1.5 adiciona o método 
        extensionClass.Methods.Add(queryMethod);

        // 2.2 Criação do método select para enumerable
        
        // 2.2.1 cria tipo de retorno
        var enumerableType = new TypeDescriptor(
            $"IEnumerable<{match.OriginType.Name}>",
            [match.OriginType.Namespaces[0], "System.Collections.Generic"],
            null);

        // 2.2.2 cria o gerador para o método
        var enumerableMethod = new MethodGenerator($"Select{match.OriginType.Name}", enumerableType);
        enumerableMethod.Attributes.Add(new RawLinesGeneratorNode(
            $"/// <summary>Projects the <see cref=\"{match.TargetType.Name}\"/> items into <see cref=\"{match.OriginType.Name}\"/>.</summary>",
            "/// <param name=\"enumerable\">The source items.</param>",
            $"/// <returns>An <see cref=\"IEnumerable{{T}}\"/> of <see cref=\"{match.OriginType.Name}\"/>.</returns>"));
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

        // 2.2.4 cria o comando do método
        var invokeSelectEnumerable = new MethodInvokeGenerator(
            enumerableParam.Name,
            "Select",
            $"{match.OriginType.Name}.From");

        enumerableMethod.Commands.Add(new ReturnCommand(invokeSelectEnumerable));

        // 2.2.5 adiciona o método
        extensionClass.Methods.Add(enumerableMethod);

        // 2.3 Cria método To{Origin} a partir do Target

        // 2.3.1 cria o método
        var toMethod = new MethodGenerator($"To{match.OriginType.Name}", match.OriginType);
        var toParamName = targetTypeIdentifier.ToLowerCamelCase();
        toMethod.Attributes.Add(new RawLinesGeneratorNode(
            $"/// <summary>Projects a <see cref=\"{match.TargetType.Name}\"/> instance into a new <see cref=\"{match.OriginType.Name}\"/>.</summary>",
            $"/// <param name=\"{toParamName}\">The source instance to project.</param>",
            $"/// <returns>A new <see cref=\"{match.OriginType.Name}\"/> instance.</returns>"));
        toMethod.Modifiers.Public();
        toMethod.Modifiers.Static();
        toMethod.UseArrow = true;

        // 2.3.2 cria o parâmetro do método: Target target
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

        // 2.4 Gera o código da classe de extensão
        extensionClass.Generate(context);
    }

    private static bool HasAccessibleBaseMember(ITypeSymbol? type, string memberName)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        for (var baseType = namedType.BaseType;
             baseType is not null && baseType.SpecialType != SpecialType.System_Object;
             baseType = baseType.BaseType)
        {
            if (baseType.GetMembers(memberName).Any(member =>
                    IsAccessibleFromDerivedType(member, namedType)))
            {
                return true;
            }

            var autoSelectAttribute = baseType.GetAttributes().FirstOrDefault(attribute =>
            {
                var attributeType = attribute.AttributeClass?.OriginalDefinition;
                return attributeType?.MetadataName == "AutoSelectAttribute`1" &&
                       attributeType.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector";
            });
            var sourceType = autoSelectAttribute?.AttributeClass?.TypeArguments.FirstOrDefault();
            if (sourceType is null)
            {
                continue;
            }

            if (memberName == "From" || memberName == $"Select{sourceType.Name}Expression")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAccessibleFromDerivedType(ISymbol member, INamedTypeSymbol derivedType)
    {
        var hasInternalAccess = SymbolEqualityComparer.Default.Equals(
                                    member.ContainingAssembly,
                                    derivedType.ContainingAssembly) ||
                                member.ContainingAssembly?.GivesAccessTo(derivedType.ContainingAssembly) == true;

        return member.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Protected => true,
            Accessibility.ProtectedOrInternal => true,
            Accessibility.Internal => hasInternalAccess,
            Accessibility.ProtectedAndInternal => hasInternalAccess,
            _ => false,
        };
    }
}
