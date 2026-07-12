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

        // Verifica se a propriedade possui o atributo AutoDetails.
        var autoDetailsAttribute = property.Symbol?.GetAttributes().FirstOrDefault(attr =>
        {
            var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return attrName == AutoDetailsAttributeName;
        });

        if (autoDetailsAttribute is null)
            return false;

        // Obtém em fromType a propriedade que corresponde ao nome.
        var fromProperty = fromType.CreateProperties(p => p.Name == property.Name && p.GetMethod is not null).FirstOrDefault();

        // Se não for encontrada, retorna um diagnóstico de erro.
        if (fromProperty is null)
        {
            var location = property.Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propertySyntax
                    ? propertySyntax.Identifier.GetLocation()
                    : property.Symbol?.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
            autoDetailInfo = new AutoDetailsInformation(
                Diagnostic.Create(AnalyzerDiagnostics.PropertyNotMatch, location, property.Name));
            return true;
        }

        // Obtém os descritores dos tipos.
        var propertyType = property.Type;
        var fromPropertyType = fromProperty.Type;

        // Cria as informações de propriedades automáticas.
        var autoPropertiesInfo = AutoPropertiesGenerator.CreateInformation(propertyType, fromPropertyType, autoDetailsAttribute);

        // Atualiza o namespace do tipo gerado com o namespace do tipo que declara a propriedade.
        var propertyNamespace = property.Symbol?.ContainingNamespace?.ToDisplayString();
        if (propertyNamespace is not null)
            propertyType.Namespaces[0] = propertyNamespace;

        // Define as propriedades conhecidas para as informações de AutoDetails.
        propertyType.DefinedProperties = autoPropertiesInfo.Properties;

        // Cria as informações de AutoDetails.
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
        GeneratedSourceConventions.ApplyRequiredNamespaces(detailsClass);

        // 1.1 - Modificadores (usa a mesma acessibilidade do tipo de origem)
        GeneratedSourceConventions.ApplyDeclaredAccessibility(detailsClass, originType.Symbol);

        // 1.2 - partial, para o desenvolvedor poder estender
        detailsClass.Modifiers.Partial();

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
        detailsClass.FileName = GeneratedSourceConventions.FileName(
            originType,
            className,
            "AutoDetails");
        detailsClass.Generate(context);
    }
}
