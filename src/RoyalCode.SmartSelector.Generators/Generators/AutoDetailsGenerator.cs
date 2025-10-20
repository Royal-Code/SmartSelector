using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoDetailsGenerator
{
    private const string AutoDetailsAttributeName = "AutoDetailsAttribute";

    internal static bool TryCreate(
        PropertyDescriptor property,
        TypeDescriptor fromType,
        out AutoDetailsInformation? autoDetailInfo)
    {
        autoDetailInfo = null;

        // verify if the property has AutoDetails attribute
        var autoDetailsAttribute = property.Symbol?.GetAttributes().FirstOrDefault(attr =>
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return attrName == AutoDetailsAttributeName;
        });

        if (autoDetailsAttribute is null)
            return false;

        // get the property of the fromType that matches the property name
        var fromProperty = fromType.CreateProperties(p => p.Name == property.Name && p.GetMethod is not null).FirstOrDefault();

        // if not found, return error diagnostic
        if (fromProperty is null)
        {
            autoDetailInfo = new AutoDetailsInformation(
                Diagnostic.Create(AnalyzerDiagnostics.PropertyNotMatch, null, property.Name));
            return true;
        }

        // get type descriptors
        var propertyType = property.Type;
        var fromPropertyType = fromProperty.Type;

        // create auto properties information
        var autoPropertiesInfo = AutoPropertiesGenerator.CreateInformation(propertyType, fromPropertyType, autoDetailsAttribute);

        // try update the namespace for the generated type with same namespaces of the property declaring type.
        var propertyNamespace = property.Symbol?.ContainingNamespace?.ToDisplayString();
        if (propertyNamespace is not null)
            propertyType.Namespaces[0] = propertyNamespace;

        // set the defined properties for the auto detail info
        propertyType.DefinedProperties = autoPropertiesInfo.Properties;

        // create the auto detail info
        autoDetailInfo = new AutoDetailsInformation(
            $"{fromPropertyType.Name}Details",
            autoPropertiesInfo);

        return true;
    }

    internal static void Generate(
        SourceProductionContext context,
        string className, 
        AutoPropertiesInformation autoPropertiesInformation)
    {
        // Gera uma nova classe DTO contendo as propriedades de autoPropertiesInformation.
        // Segue o mesmo estilo de geração usado por AutoPropertiesGenerator.Generate.

        var originType = autoPropertiesInformation.OriginType;
        var properties = autoPropertiesInformation.Properties;

        // Se não há propriedades, não gera nada.
        if (originType == null || properties.Length == 0)
            return;

        // 1 - Criação da classe
        var detailsClass = new ClassGenerator(className, originType.Namespaces[0]);
        

        // 1.1 - Modificadores (usa a mesma acessibilidade do tipo de origem)
        if (originType.Symbol?.DeclaredAccessibility == Accessibility.Public)
        {
            detailsClass.Modifiers.Public();
        }
        else if (originType.Symbol?.DeclaredAccessibility == Accessibility.Internal)
        {
            detailsClass.Modifiers.Internal();
        }
        else if (originType.Symbol?.DeclaredAccessibility == Accessibility.Protected)
        {
            detailsClass.Modifiers.Protected();
        }
        else if (originType.Symbol?.DeclaredAccessibility == Accessibility.Private)
        {
            detailsClass.Modifiers.Private();
        }

        // 1.2 - partial, para o desenvolvedor poder estender
        detailsClass.Modifiers.Public();

        // 2 - Criação das propriedades públicas com get/set
        foreach (var p in properties)
        {
            var propertyType = p.Type.HasNamedTypeSymbol(out var typeSymbol)
                ? TypeDescriptor.Create(typeSymbol)
                : p.Type;

            var prop = new PropertyGenerator(propertyType, p.Name);
            prop.Modifiers.Public();

            detailsClass.Properties.Add(prop);
        }

        // 3 - Configura o nome do arquivo e gera
        detailsClass.FileName = $"{className}.AutoDetails.g.cs";
        detailsClass.Generate(context);
    }
}
