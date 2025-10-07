using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoPropertyGenerator
{
    internal static MatchOptions MatchOptions { get; } = new()
    {
        OriginPropertiesRetriever = new AutoPropertyOriginPropertiesRetriever(),
        //TargetPropertiesRetriever = new AutoPropertyTargetPropertiesRetriever(),
    };

    internal static AutoPropertyInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        AttributeSyntax properties)
    {
        // collect excluded property names using extension helpers
        var excluded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in properties.GetConstructorStringSet())
            excluded.Add(name);

        foreach (var name in properties.GetNamedArgumentStringSet("Exclude"))
            excluded.Add(name);

        return CreateInformation(modelType, fromType, excluded);
    }

    internal static AutoPropertyInformation CreateInformation(
        TypeDescriptor modelType,
        ITypeSymbol fromType,
        AttributeData autoPropertyAttribute)
    {
        // obtém excluded do ctor ou propriedade Exclude
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in autoPropertyAttribute.ConstructorArguments)
            if (name.Kind == TypedConstantKind.Array && name.Values != null)
                foreach (var v in name.Values)
                    if (v.Value is string s)
                        excluded.Add(s);
        foreach (var namedArg in autoPropertyAttribute.NamedArguments)
            if (namedArg.Key == "Exclude" && namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values != null)
                foreach (var v in namedArg.Value.Values)
                    if (v.Value is string s)
                        excluded.Add(s);

        // gera o TypeDescriptor do fromType
        var fromTypeDescriptor = TypeDescriptor.Create(fromType);

        return CreateInformation(modelType, fromTypeDescriptor, excluded);
    }

    internal static AutoPropertyInformation CreateInformation(
        TypeDescriptor modelType,
        TypeDescriptor fromType,
        HashSet<string> excluded)
    {
        // declared properties in model type are always excluded
        foreach (var p in modelType.CreateProperties(p => p.SetMethod is not null))
            excluded.Add(p.Name);

        var sourceProps = fromType.CreateProperties(p => p.GetMethod is not null);
        var generated = new List<PropertyDescriptor>();
        foreach (var p in sourceProps)
        {
            if (excluded.Contains(p.Name))
                continue;
            generated.Add(new PropertyDescriptor(p.Type, p.Name, p.Symbol));
        }

        return new AutoPropertyInformation(modelType, [.. generated]);
    }

    internal static void Generate(AutoPropertyInformation propertiesInfo, SourceProductionContext context)
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
            var prop = new PropertyGenerator(p.Type, p.Name);
            
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
    public IReadOnlyList<PropertyDescriptor> GetProperties(TypeDescriptor origin)
    {
        var typeSymbol = origin.Symbol;
        if (typeSymbol == null)
            return MatchOptions.GetOriginProperties(origin);

        // Verifica se no type existe:
        // - o AutoPropertiesAttribute com AutoSelectAttribute<TFrom>
        // - ou AutoPropertiesAttribute<TFrom>
        var atrributes = typeSymbol.GetAttributes();

        // obtém o AutoPropertiesAttribute
        var autoPropertiesAttribute = atrributes.FirstOrDefault(
            attr => attr.AttributeClass?.ToDisplayString() == "RoyalCode.SmartSelector.AutoPropertiesAttribute");

        if (autoPropertiesAttribute is not null)
        {
            // quando tem AutoPropertiesAttribute, deve ter o AutoSelectAttribute<TFrom>
            var autoSelectAttribute = atrributes.FirstOrDefault(
                attr => attr.AttributeClass?.ToDisplayString() == "RoyalCode.SmartSelector.AutoSelectAttribute`1");

            // se não tiver, retorna o padrão
            if (autoSelectAttribute == null)
                return MatchOptions.GetOriginProperties(origin);

            // obtém o tipo TFrom
            var fromType = autoSelectAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
            if (fromType == null)
                return MatchOptions.GetOriginProperties(origin);

            // cria a informação
            var info = AutoPropertyGenerator.CreateInformation(origin, fromType, autoSelectAttribute);

            // pega as propriedades da origem mais as da informação
            return [.. MatchOptions.GetOriginProperties(origin), .. info.Properties];
        }

        // obtém o AutoPropertiesAttribute<TFrom>
        autoPropertiesAttribute = atrributes.FirstOrDefault(
            attr => attr.AttributeClass?.ToDisplayString() == "RoyalCode.SmartSelector.AutoPropertiesAttribute`1");

        if (autoPropertiesAttribute is not null)
        {
            // obtém o tipo TFrom
            var fromType = autoPropertiesAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
            if (fromType == null)
                return MatchOptions.GetOriginProperties(origin);

            // cria a informação
            var info = AutoPropertyGenerator.CreateInformation(origin, fromType, autoPropertiesAttribute);

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