using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoPropertiesGenerator
{
    public const string AutoPropertiesAttributeTypedFullName = "RoyalCode.SmartSelector.AutoPropertiesAttribute`1";

    internal static MatchOptions MatchOptions { get; } = new()
    {
        OriginPropertiesRetriever = new AutoPropertyOriginPropertiesRetriever(),
        //TargetPropertiesRetriever = new AutoPropertyTargetPropertiesRetriever(),
    };

    internal static bool Predicate(SyntaxNode node, CancellationToken token)
    {
        var accept = node is ClassDeclarationSyntax;
        return accept;
    }

    internal static AutoPropertiesInformation Transform(
        GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        // ainda não implementado
        return new(null, null);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        AttributeSyntax properties)
    {
        // collect excluded property names using extension helpers
        var excluded = new HashSet<string>(StringComparer.Ordinal);

        // removido por hora
        ////foreach (var name in properties.GetConstructorStringSet())
        ////    excluded.Add(name);

        foreach (var name in properties.GetNamedArgumentStringSet("Exclude"))
            excluded.Add(name);

        return CreateInformation(modelType, fromType, excluded);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        ITypeSymbol fromType,
        AttributeData autoPropertyAttribute)
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal);

        // removido por hora
        ////// obtém excluded do ctor ou propriedade Exclude
        ////foreach (var name in autoPropertyAttribute.ConstructorArguments)
        ////    if (name.Kind == TypedConstantKind.Array && name.Values != null)
        ////        foreach (var v in name.Values)
        ////            if (v.Value is string s)
        ////                excluded.Add(s);

        // obtém Exclude de NamedArguments
        foreach (var namedArg in autoPropertyAttribute.NamedArguments)
            if (namedArg.Key == "Exclude" && namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values != null)
                foreach (var v in namedArg.Value.Values)
                    if (v.Value is string s)
                        excluded.Add(s);

        // gera o TypeDescriptor do fromType
        var fromTypeDescriptor = fromType.CreateTypeDescriptor();

        return CreateInformation(modelType, fromTypeDescriptor, excluded);
    }

    internal static AutoPropertiesInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        HashSet<string> excluded)
    {
        // declared properties in model type are always excluded
        foreach (var p in modelType.CreateProperties(p => p.SetMethod is not null))
            excluded.Add(p.Name);

        var sourceProps = fromType.CreateProperties(p => p.GetMethod is not null);

        // filtra propriedades do source,
        // remove propriedades que estão na lista de excluídas,
        // removendo o que não for tipo primitivo, string, decimal, DateTime,
        // enum ou nullable desses tipos, além de coleções de tipos primitivos,
        // aceita structs também.
        sourceProps = sourceProps
            .Where(p => !excluded.Contains(p.Name))
            .Where(IsSupportedType)
            .ToArray();

        var generated = new List<PropertyDescriptor>();
        foreach (var p in sourceProps)
        {
            generated.Add(new PropertyDescriptor(p.Type, p.Name, p.Symbol));
        }

        return new AutoPropertiesInformation(modelType, [.. generated]);
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
                var argType = arg.CreateTypeDescriptor();
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
                ? typeSymbol.CreateTypeDescriptor()
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
        var typeSymbol = origin.Symbol;
        if (typeSymbol == null)
            return MatchOptions.GetOriginProperties(origin);

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
                return MatchOptions.GetOriginProperties(origin);

            // obtém o tipo TFrom
            var fromType = autoSelectAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
            if (fromType == null)
                return MatchOptions.GetOriginProperties(origin);

            // cria a informação
            var info = AutoPropertiesGenerator.CreateInformation(origin, fromType, autoSelectAttribute);

            // pega as propriedades da origem mais as da informação
            return [.. MatchOptions.GetOriginProperties(origin), .. info.Properties];
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
                return MatchOptions.GetOriginProperties(origin);

            // cria a informação
            var info = AutoPropertiesGenerator.CreateInformation(origin, fromType, autoPropertiesAttribute);

            // pega as propriedades da origem mais as da informação
            return [.. MatchOptions.GetOriginProperties(origin), .. info.Properties];
        }

        return MatchOptions.GetOriginProperties(origin);
    }
}

//internal class AutoPropertyTargetPropertiesRetriever : ITargetPropertiesRetriever
//{
//    public IReadOnlyList<PropertyDescriptor> GetProperties(TypeDescriptor target)
//    {
//        throw new NotImplementedException();
//    }
//}