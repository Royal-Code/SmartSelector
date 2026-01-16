using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoPropertiesGenerator
{
    public const string AutoPropertiesAttributeTypedFullName = "RoyalCode.SmartSelector.AutoPropertiesAttribute`1";

    private const string AutoPropertiesAttributeName = "AutoProperties";              // non generic form
    private const string AutoPropertiesGenericAttributeName = "AutoProperties";       // generic form base identifier
    internal const string MapFromAttributeName = "MapFromAttribute";

    internal static MatchOptions MatchOptions { get; } = new()
    {
        OriginPropertiesRetriever = new AutoPropertyOriginPropertiesRetriever(),
        AdditionalAssignDescriptorResolvers = [new AutoDetailsAssignDescriptorResolver()],
        PropertyNameResolvers = [new MapFromPropertyNameResolver()],
    };

    internal static bool Predicate(SyntaxNode node, CancellationToken token)
    {
        var accept = node is ClassDeclarationSyntax;
        return accept;
    }

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
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        // Recupera TODOS os atributos de AutoProperties na declaração (genérico e não genérico)
        var allAutoProps = classDeclaration.AttributeLists
            .SelectMany(l => l.Attributes)
            .Where(a => a.Name is IdentifierNameSyntax id && id.Identifier.Text == AutoPropertiesAttributeName
                        || a.Name is GenericNameSyntax g && g.Identifier.Text == AutoPropertiesGenericAttributeName)
            .ToArray();

        if (allAutoProps.Length == 0)
        {
            // Não deveria acontecer pois o pipeline só chama se tem o atributo, mas retorna vazio por segurança.
            return new AutoPropertiesInformation(
                classSymbol is null
                    ? null!
                    : new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol),
                []);
        }

        // Verifica conflito: uso simultâneo do genérico e não-genérico.
        bool hasNonGeneric = allAutoProps.Any(a => a.Name is IdentifierNameSyntax);
        if (hasNonGeneric)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.ConflictingAutoPropertiesAttributes,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
            return new AutoPropertiesInformation(diagnostic);
        }

        // Cria TypeDescriptor do modelo
        var modelType = new TypeDescriptor(classDeclaration.Identifier.Text, [classDeclaration.GetNamespace()], classSymbol);

        // Localiza o AttributeSyntax exato para extrair parâmetros nomeados (Exclude)
        var attrSyntax = allAutoProps.First(a => a.Name is GenericNameSyntax);

        var genericAttr = (GenericNameSyntax)attrSyntax.Name;

        // Pega o argumento de tipo TFrom
        if (genericAttr.TypeArgumentList.Arguments.Count != 1)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                classDeclaration.Identifier.GetLocation(),
                "<missing>");
            return new AutoPropertiesInformation(diagnostic);
        }

        var fromTypeSyntax = genericAttr.TypeArgumentList.Arguments[0];

        // Tentamos criar TypeDescriptor para o tipo origem
        var fromType = TypeDescriptor.Create(fromTypeSyntax, context.SemanticModel);
        if (fromType.Symbol is null)
        {
            var diagnostic = Diagnostic.Create(AnalyzerDiagnostics.InvalidAutoPropertiesTypeArgument,
                fromTypeSyntax.GetLocation(),
                fromTypeSyntax.ToString());
            return new AutoPropertiesInformation(diagnostic);
        }

        // Cria a informação
        return CreateInformation(modelType, fromType, attrSyntax);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        AttributeSyntax autoPropertyAttribute)
    {
        // collect excluded property names using extension helpers
        var excluded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in autoPropertyAttribute.GetNamedArgumentStrings("Exclude"))
            excluded.Add(name);

        // collect flattening property names using extension helpers
        HashSet<string>? flattening = null;
        var flatteningNames = autoPropertyAttribute.GetNamedArgumentStrings("Flattening");
        foreach (var name in flatteningNames)
        {
            flattening ??= new HashSet<string>(StringComparer.Ordinal);
            flattening.Add(name);
        }

        return CreateInformation(modelType, fromType, excluded, flattening);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        ITypeSymbol fromType,
        AttributeData autoPropertyAttribute)
    {
        // gera o TypeDescriptor do fromType
        var fromTypeDescriptor = TypeDescriptor.Create(fromType);

        return CreateInformation(modelType, fromTypeDescriptor, autoPropertyAttribute);
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
            if (namedArg.Key == "Exclude" && namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values != null)
            {
                foreach (var v in namedArg.Value.Values)
                    if (v.Value is string s)
                        excluded.Add(s);
            }
            else if (namedArg.Key == "Flattening" && namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values != null)
            {
                foreach (var fv in namedArg.Value.Values)
                    if (fv.Value is string fs)
                        flattening.Add(fs);
            }

        return CreateInformation(modelType, fromType, excluded, flattening);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        HashSet<string> excluded,
        HashSet<string>? flattening)
    {
        var autoDetails = new List<AutoDetailsInformation>();

        // declared properties in model type are always excluded
        foreach (var p in modelType.CreateProperties(p => p.SetMethod is not null))
        {
            excluded.Add(p.Name);

            // processa se propriedade se tem atributo AutoDetails
            if(AutoDetailsGenerator.TryCreate(p, fromType, out var autoDetailInfo))
            {
                autoDetails.Add(autoDetailInfo!);
            }
        }
        var sourceProps = fromType.CreateProperties(p => p.GetMethod is not null);

        // filtra propriedades do source,
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
        var typeName = type.Name;

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

        // 1.1 modificadores
        if (origin.Symbol?.DeclaredAccessibility == Accessibility.Public)
        {
            partialClass.Modifiers.Public();
        }
        if (origin.Symbol?.DeclaredAccessibility == Accessibility.Internal)
        {
            partialClass.Modifiers.Internal();
        }
        if (origin.Symbol?.DeclaredAccessibility == Accessibility.Protected)
        {
            partialClass.Modifiers.Protected();
        }
        if (origin.Symbol?.DeclaredAccessibility == Accessibility.Private)
        {
            partialClass.Modifiers.Private();
        }

        partialClass.Modifiers.Partial();

        // 2 - criação das propriedades
        foreach (var p in properties)
        {
            var propertyType = p.Type.HasNamedTypeSymbol(out var typeSymbol)
                ? TypeDescriptor.Create(typeSymbol)
                : p.Type;

            var prop = new PropertyGenerator(propertyType, p.Name);

            // 2.1 modificadores
            prop.Modifiers.Public();

            // 2.3 adiciona a propriedade na classe
            partialClass.Properties.Add(prop);
        }

        // 3 Gera o código da classe
        partialClass.FileName = $"{origin.Name}.AutoProperties.g.cs";
        partialClass.Generate(context);
    }
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

            // cria a informação
            var info = AutoPropertiesGenerator.CreateInformation(origin, fromType, autoSelectAttribute);

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
            var info = AutoPropertiesGenerator.CreateInformation(origin, fromType, autoPropertiesAttribute);

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

        // check if the left type has defined properties
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

        // faz o match entre as propriedades
        // match das propriedades da classe com o attributo e a classe definida no TFrom.
        var matchSelection = MatchSelection.Create(leftType, leftProperties, rightType, rightProperties, model, options);

        // se tem problemas, não é possível fazer o match.
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

        // check if the symbol has the MapFrom attribute
        var attr = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == AutoPropertiesGenerator.MapFromAttributeName);

        if (attr is not null)
        {
            // First, try constructor argument (preferred usage)
            if (attr.ConstructorArguments.Length == 1)
            {
                propertyName = attr.ConstructorArguments[0].Value as string;
                if (!string.IsNullOrEmpty(propertyName))
                    return true;
            }

            // Fallback: try named argument on the attribute (PropertyName setter)
            if (attr.NamedArguments.Length > 0)
            {
                var named = attr.NamedArguments.FirstOrDefault(a => a.Key == "PropertyName");
                if (named.Value.Value is string s && !string.IsNullOrEmpty(s))
                {
                    propertyName = s;
                    return true;
                }
            }

            // Last resort: inspect declaration syntax to extract literal or nameof
            // This handles cases where Roslyn didn't materialize ConstructorArguments in this pipeline
            foreach (var decl in symbol.DeclaringSyntaxReferences)
            {
                var syntax = decl.GetSyntax();
                if (syntax is not PropertyDeclarationSyntax pds)
                    continue;

                foreach (var al in pds.AttributeLists)
                {
                    foreach (var a in al.Attributes)
                    {
                        var nameText = a.Name switch
                        {
                            IdentifierNameSyntax id => id.Identifier.Text,
                            GenericNameSyntax gn => gn.Identifier.Text,
                            _ => a.Name.ToString()
                        };

                        if (!string.Equals(nameText, "MapFrom", StringComparison.Ordinal))
                            continue;

                        if (a.ArgumentList is { Arguments.Count: > 0 })
                        {
                            var expr = a.ArgumentList.Arguments[0].Expression;
                            switch (expr)
                            {
                                case LiteralExpressionSyntax les:
                                    var text = les.Token.ValueText;
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        propertyName = text;
                                        return true;
                                    }
                                    break;
                                case InvocationExpressionSyntax ies:
                                    // Handle nameof(Member) -> get last identifier as property name
                                    if (ies.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax ident
                                        && string.Equals(ident.Identifier.Text, "nameof", StringComparison.Ordinal)
                                        && ies.ArgumentList.Arguments.Count == 1)
                                    {
                                        var argExpr = ies.ArgumentList.Arguments[0].Expression;
                                        if (argExpr is IdentifierNameSyntax idArg)
                                        {
                                            propertyName = idArg.Identifier.Text;
                                            if (!string.IsNullOrEmpty(propertyName))
                                                return true;
                                        }
                                        else if (argExpr is MemberAccessExpressionSyntax mae)
                                        {
                                            propertyName = mae.Name.Identifier.Text;
                                            if (!string.IsNullOrEmpty(propertyName))
                                                return true;
                                        }
                                    }
                                    break;
                                case MemberAccessExpressionSyntax maeExpr:
                                    // If provided directly as something like Product.Name (rare), take the right side
                                    propertyName = maeExpr.Name.Identifier.Text;
                                    if (!string.IsNullOrEmpty(propertyName))
                                        return true;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}