using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoPropertiesGenerator
{
    public const string AutoPropertiesAttributeTypedFullName = "RoyalCode.SmartSelector.AutoPropertiesAttribute`1";
    public const string AutoPropertiesAttributeFullName = "RoyalCode.SmartSelector.AutoPropertiesAttribute";

    internal const string MapFromAttributeName = "MapFromAttribute";

    internal static MatchOptions MatchOptions { get; } = new()
    {
        OriginPropertiesRetriever = new AutoPropertyOriginPropertiesRetriever(),
        AdditionalAssignDescriptorResolvers = [new AutoDetailsAssignDescriptorResolver()],
        PropertyNameResolvers = [new MapFromPropertyNameResolver()],
    };

    internal static AutoPropertiesInformation Transform(
        GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        // Classe alvo (onde o atributo está aplicado)
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        // Obtém symbol da classe
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, token);
        if (classSymbol is null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        // A classe deve ser partial (seguindo o padrão usado pelo AutoSelect)
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.AutoPropertiesRequiresPartialClass,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        if (classSymbol.Arity > 0 || HasGenericContainingType(classSymbol))
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.GenericDestinationTypeNotSupported,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            return new AutoPropertiesInformation(diagnostic);
        }

        if (FindNonPartialContainingType(classDeclaration) is { } nonPartialContainingType)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.AutoPropertiesRequiresPartialClass,
                classDeclaration.Identifier.GetLocation(),
                nonPartialContainingType.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        if (classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.GlobalNamespaceNotSupported,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            return new AutoPropertiesInformation(diagnostic);
        }

        var autoPropertiesAttribute = context.Attributes.FirstOrDefault(attribute =>
            attribute.AttributeClass?.MetadataName == "AutoPropertiesAttribute`1" &&
            attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");
        var attributeSyntax = autoPropertiesAttribute?.ApplicationSyntaxReference?.GetSyntax(token)
            as AttributeSyntax;
        if (autoPropertiesAttribute is null)
        {
            var diagnostic = Diagnostic.Create(
                AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                classDeclaration.Identifier.GetLocation(),
                "<missing>");
            return new AutoPropertiesInformation(diagnostic);
        }

        // Verifica semanticamente o conflito entre as formas genérica e não genérica.
        var hasNonGeneric = classSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.MetadataName == "AutoPropertiesAttribute" &&
            attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");
        if (hasNonGeneric)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.ConflictingAutoPropertiesAttributes,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol);
        var fromSymbol = autoPropertiesAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
        if (fromSymbol is null ||
            fromSymbol.TypeKind is not TypeKind.Class and not TypeKind.Struct)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                attributeSyntax?.Name.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
                fromSymbol?.ToDisplayString() ?? "<missing>");
            return new AutoPropertiesInformation(diagnostic);
        }

        return CreateInformation(modelType, TypeDescriptor.Create(fromSymbol), autoPropertiesAttribute);
    }

    internal static Diagnostic? ValidateNonGenericUsage(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, token);
        if (classSymbol is null)
        {
            return null;
        }

        var hasAutoSelect = classSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.OriginalDefinition.MetadataName == "AutoSelectAttribute`1" &&
            attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");
        if (hasAutoSelect)
        {
            return null;
        }

        var attributeSyntax = context.Attributes.FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax(token)
            as AttributeSyntax;
        return Diagnostic.Create(
            AnalyzerDiagnostics.AutoPropertiesRequiresAutoSelect,
            attributeSyntax?.Name.GetLocation() ?? classDeclaration.Identifier.GetLocation(),
            classSymbol.Name);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        AttributeData autoPropertyAttribute)
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        var flattening = new HashSet<string>(StringComparer.Ordinal);

        // obtém Exclude de NamedArguments
        foreach (var namedArg in autoPropertyAttribute.NamedArguments)
            if (namedArg.Key == "Exclude" &&
                namedArg.Value.Kind == TypedConstantKind.Array &&
                !namedArg.Value.IsNull)
            {
                foreach (var v in namedArg.Value.Values)
                    if (v.Value is string s)
                        excluded.Add(s);
            }
            else if (namedArg.Key == "Flattening" &&
                     namedArg.Value.Kind == TypedConstantKind.Array &&
                     !namedArg.Value.IsNull)
            {
                foreach (var fv in namedArg.Value.Values)
                    if (fv.Value is string fs)
                        flattening.Add(fs);
            }

        return CreateInformationCore(modelType, fromType, excluded, flattening);
    }

    private static AutoPropertiesInformation CreateInformationCore(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        HashSet<string> excluded,
        HashSet<string>? flattening)
    {
        var autoDetails = new List<AutoDetailsInformation>();
        var autoDetailsTypeKeys = new HashSet<string>(StringComparer.Ordinal);

        // Propriedades já declaradas no modelo são sempre excluídas.
        foreach (var p in modelType.CreateProperties(_ => true))
        {
            excluded.Add(p.Name);

            // processa a propriedade se ela tem o atributo AutoDetails
            if(AutoDetailsGenerator.TryCreate(p, fromType, autoDetailsTypeKeys, out var autoDetailInfo))
            {
                autoDetails.Add(autoDetailInfo!);
            }
        }
        var sourceProps = fromType.CreateProperties(p => p.GetMethod is not null);

        // filtra propriedades da origem,
        // remove propriedades que estão na lista de excluídas,
        // remove propriedades de flattening (serão recriadas depois)
        // removendo o que não for tipo primitivo, string, decimal, DateTime,
        // enum ou nullable desses tipos, além de coleções de tipos primitivos,
        // aceita structs também.
        var autoProps = sourceProps
            .Where(p => !excluded.Contains(p.Name))
            .Where(p => flattening is null || !flattening.Contains(p.Name))
            .Where(IsSupportedType)
            .ToList();

        // processa flattening se houver
        if (flattening is not null)
            foreach (var fp in CreateFlattening(fromType, sourceProps, flattening))
                if (!autoProps.Any(p => p.Name == fp.Name))
                    autoProps.Add(fp);

        var generated = new List<PropertyDescriptor>();
        foreach (var p in autoProps)
        {
            generated.Add(new PropertyDescriptor(p.Type, p.Name, p.Symbol));
        }

        return new AutoPropertiesInformation(modelType, [.. generated], [.. autoDetails]);
    }

    private static IReadOnlyList<PropertyDescriptor> CreateFlattening(
        TypeDescriptor fromType,
        IReadOnlyList<PropertyDescriptor> sourceProps,
        HashSet<string> flattening)
    {
        var list = new List<PropertyDescriptor>();
        var matchTypeInfo = new MatchTypeInfo(fromType, sourceProps, MatchOptions.Default);

        foreach (var flattenPropName in flattening)
        {
            var flattenProp = new PropertyDescriptor(TypeDescriptor.Void(), flattenPropName, null);
            var selection = PropertySelection.Select(flattenProp, matchTypeInfo);

            if (selection == null)
                continue;

            if (!selection.PropertyType.Type.HasNamedTypeSymbol(out var namedType))
                continue;

            // se não for classe ou struct, não faz flattening
            if (namedType.TypeKind != TypeKind.Class && namedType.TypeKind != TypeKind.Struct)
                continue;

            // obtém as propriedades do tipo
            var nestedProps = TypeDescriptor.Create(namedType).CreateProperties(p => p.GetMethod is not null);
            foreach (var np in nestedProps.Where(IsSupportedType))
            {
                // cria nova propriedade com o nome composto
                var newPropName = $"{flattenPropName}{np.Name}";
                list.Add(new PropertyDescriptor(np.Type, newPropName, np.Symbol));
            }
        }

        return list;
    }

    private static readonly HashSet<string> SupportedPrimitiveTypes = new(StringComparer.Ordinal)
    {
        "bool",
        "Boolean",
        "string", "char",
        "String", "Char",
        "byte", "short", "int", "long", "float", "double", "decimal",
        "Byte", "Int16", "Int32", "Int64", "Single", "Double", "Decimal",
        "sbyte", "ushort", "uint", "ulong",
        "SByte", "UInt16", "UInt32", "UInt64",
        "DateTime", "DateTime?",
    };

    private static bool IsSupportedType(PropertyDescriptor descriptor)
    {
        var type = descriptor.Type;

        // O nome pode carregar a anotação nullable ('string?'); o tipo subjacente decide o suporte.
        var typeName = type.UnderlyingType;

        if (SupportedPrimitiveTypes.Contains(typeName))
            return true;

        if (!type.HasNamedTypeSymbol(out var namedType))
            return false;

        // se tem symbol, verifica se é enum
        if (namedType.TypeKind == TypeKind.Enum)
            return true;

        // struct é considerado um value object, não é um tipo complexo e deve ser aceito
        if (namedType.TypeKind == TypeKind.Struct)
            return true;

        // se for genérico, verifica se coleção suportada
        if (namedType.IsGenericType)
        {
            // verifica se namedType implementa ou herda IEnumerable<>
            if (!namedType.AllInterfaces.Any(i =>
                i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
            {
                return false;
            }

            var arg = namedType.TypeArguments.FirstOrDefault();
            if (arg != null)
            {
                var argType = TypeDescriptor.Create(arg);
                return SupportedPrimitiveTypes.Contains(argType.Name)
                    || arg.TypeKind == TypeKind.Enum
                    || arg.TypeKind == TypeKind.Struct;
            }
        }

        return false;
    }

    internal static void Generate(AutoPropertiesInformation propertiesInfo, SourceProductionContext context)
    {
        // gera o código de propriedades automáticas em uma classe partial

        if (propertiesInfo.OriginType == null || propertiesInfo.Properties.Length == 0)
            return;

        var origin = propertiesInfo.OriginType;
        var properties = propertiesInfo.Properties;

        // 1 - criação da classe partial
        var partialClass = new ClassGenerator(origin.Name, origin.Namespaces[0]);
        GeneratedSourceConventions.ApplyRequiredNamespaces(partialClass);
        GeneratedSourceConventions.ApplyContainingTypes(
            partialClass,
            origin.Symbol as INamedTypeSymbol);

        // 1.1 modificadores
        GeneratedSourceConventions.ApplyDeclaredAccessibility(partialClass, origin.Symbol);

        partialClass.Modifiers.Partial();

        // 2 - criação das propriedades
        foreach (var p in properties)
        {
            var propertyType = p.Type.HasNamedTypeSymbol(out var typeSymbol)
                ? TypeDescriptor.Create(typeSymbol)
                : p.Type;
            propertyType = GeneratedSourceConventions.PreserveNullableAnnotation(propertyType);

            // membros gerados em tipo declarado pelo usuário: docs e [GeneratedCode] por membro (DF11)
            var prop = new AnnotatedPropertyGenerator(
                propertyType,
                p.Name,
                [
                    "/// <summary>Generated property, projected from the source type.</summary>",
                    GeneratedSourceConventions.GeneratedCodeAttributeLine,
                ]);

            // 2.1 modificadores
            prop.Modifiers.Public();

            // 2.3 adiciona a propriedade na classe
            partialClass.Properties.Add(prop);
        }

        // 3 Gera o código da classe
        partialClass.FileName = GeneratedSourceConventions.FileName(
            origin,
            origin.Name,
            "AutoProperties");
        partialClass.Generate(context);
    }

    private static bool HasGenericContainingType(INamedTypeSymbol symbol)
    {
        for (var containingType = symbol.ContainingType;
             containingType is not null;
             containingType = containingType.ContainingType)
        {
            if (containingType.Arity > 0)
                return true;
        }

        return false;
    }

    private static TypeDeclarationSyntax? FindNonPartialContainingType(ClassDeclarationSyntax declaration) =>
        declaration.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(type => !type.Modifiers.Any(SyntaxKind.PartialKeyword));
}

internal class AutoPropertyOriginPropertiesRetriever : IOriginPropertiesRetriever
{
    private const string AutoSelectAttributeName = "AutoSelectAttribute";
    private const string AutoPropertiesAttributeName = "AutoPropertiesAttribute";
    private const string TypedAutoPropertiesAttributeName = "AutoPropertiesAttribute<";

    public IReadOnlyList<PropertyDescriptor> GetProperties(TypeDescriptor origin)
    {
        var originProperties = MatchOptions.GetOriginProperties(origin);
        origin.DefinedProperties = originProperties;
        var typeSymbol = origin.Symbol;

        if (typeSymbol == null)
            return originProperties;

        // Verifica se no type existe:
        // - o AutoPropertiesAttribute com AutoSelectAttribute<TFrom>
        // - ou AutoPropertiesAttribute<TFrom>
        var attributes = typeSymbol.GetAttributes();

        // obtém o AutoPropertiesAttribute
        var autoPropertiesAttribute = attributes.FirstOrDefault(attr =>
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return attrName == AutoPropertiesAttributeName;
        });

        if (autoPropertiesAttribute is not null)
        {
            // quando tem AutoPropertiesAttribute, deve ter o AutoSelectAttribute<TFrom>
            var autoSelectAttribute = attributes.FirstOrDefault(attr =>
            {
                var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                return attrName?.StartsWith(AutoSelectAttributeName) ?? false;
            });

            // se não tiver, retorna o padrão
            if (autoSelectAttribute == null)
                return originProperties;

            // obtém o tipo TFrom
            var fromType = autoSelectAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
            if (fromType == null)
                return originProperties;

            // cria a informação; Exclude/Flattening vêm do AutoPropertiesAttribute,
            // não do AutoSelectAttribute (que não declara esses argumentos)
            var info = AutoPropertiesGenerator.CreateInformation(
                origin,
                TypeDescriptor.Create(fromType),
                autoPropertiesAttribute);

            // pega as propriedades da origem mais as da informação
            return [.. originProperties, .. info.Properties];
        }

        // obtém o AutoPropertiesAttribute<TFrom>
        autoPropertiesAttribute = attributes.FirstOrDefault(attr =>
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return attrName?.StartsWith(TypedAutoPropertiesAttributeName) ?? false;
        });

        if (autoPropertiesAttribute is not null)
        {
            // obtém o tipo TFrom
            var fromType = autoPropertiesAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
            if (fromType == null)
                return originProperties;

            // cria a informação
            var info = AutoPropertiesGenerator.CreateInformation(
                origin,
                TypeDescriptor.Create(fromType),
                autoPropertiesAttribute);

            // pega as propriedades da origem mais as da informação
            return [.. originProperties, .. info.Properties];
        }

        return originProperties;
    }
}

internal class AutoDetailsAssignDescriptorResolver : IAssignDescriptorResolver
{
    public bool TryCreateAssignDescriptor(
        TypeDescriptor leftType,
        TypeDescriptor rightType,
        SemanticModel model,
        MatchOptions options,
        out AssignDescriptor? descriptor)
    {
        descriptor = null;

        // Verifica se o tipo à esquerda possui propriedades definidas.
        if (!leftType.HasDefinedProperties())
            return false;

        // obtém propriedades do tipo de origem
        var leftProperties = options.OriginPropertiesRetriever.GetProperties(leftType);

        // valida se tem propriedades
        if (leftProperties.Count == 0)
            return false;

        // obtém propriedades do tipo de destino
        var rightProperties = options.TargetPropertiesRetriever.GetProperties(rightType);

        // valida se tem propriedades
        if (rightProperties.Count == 0)
            return false;

        // faz a correspondência entre as propriedades
        // corresponde as propriedades da classe com o atributo e a classe definida em TFrom.
        var matchSelection = MatchSelection.Create(leftType, leftProperties, rightType, rightProperties, model, options);

        // se houver problemas, não é possível concluir a correspondência.
        if (matchSelection.HasMissingProperties(out _) || matchSelection.HasNotAssignableProperties(out _))
        {
            return false;
        }

        descriptor = new AssignDescriptor()
        {
            AssignType = AssignType.NewInstance,
            InnerSelection = matchSelection
        };

        return true;
    }
}

internal class MapFromPropertyNameResolver : IPropertyNameResolver
{
    public bool TryResolvePropertyName(IPropertySymbol symbol, out string? propertyName)
    {
        propertyName = null;

        var attr = symbol.GetAttributes()
            .FirstOrDefault(attribute =>
                attribute.AttributeClass?.MetadataName == AutoPropertiesGenerator.MapFromAttributeName &&
                attribute.AttributeClass.ContainingNamespace.ToDisplayString() == "RoyalCode.SmartSelector");

        propertyName = attr is { ConstructorArguments.Length: 1 }
            ? attr.ConstructorArguments[0].Value as string
            : null;
        return !string.IsNullOrEmpty(propertyName);
    }
}
