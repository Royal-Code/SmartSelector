using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class AutoDetailsGenerator
{
    private const string AutoDetailsAttributeName = "AutoDetailsAttribute";

    internal static bool TryCreate(
        PropertyDescriptor property,
        TypeDescriptor fromType,
        HashSet<string> generatedTypeKeys,
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
            autoDetailInfo = new AutoDetailsInformation(
                DiagnosticInfo.Create(AnalyzerDiagnostics.PropertyNotMatch, GetPropertyLocation(property), property.Name),
                property.Name);
            return true;
        }

        // O tipo declarado na propriedade é a fonte de verdade do tipo gerado (DF2).
        var declaredType = property.Type;
        var fromPropertyType = fromProperty.Type;

        // Quando o tipo declarado é 'X?' e X ainda não existe, o compilador vincula Nullable<X-error>;
        // o símbolo efetivo é o argumento de tipo.
        var effectiveSymbol = declaredType.Symbol;
        if (effectiveSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableSymbol &&
            nullableSymbol.TypeArguments.FirstOrDefault() is INamedTypeSymbol nullableArgument)
        {
            effectiveSymbol = nullableArgument;
        }

        // Verifica se o tipo declarado já existe na compilação (símbolo real, não error type).
        var existingType = effectiveSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeKind != TypeKind.Error
            ? namedTypeSymbol
            : null;

        string? targetNamespace;
        if (existingType is not null)
        {
            // Um tipo existente só pode ser completado se for uma classe partial declarada nesta compilação.
            if (!IsPartialClassInSource(existingType))
            {
                autoDetailInfo = new AutoDetailsInformation(
                    DiagnosticInfo.Create(
                        AnalyzerDiagnostics.AutoDetailsTypeMustBePartial,
                        GetPropertyLocation(property),
                        existingType.Name,
                        property.Name),
                    property.Name);
                return true;
            }

            if (!IsAccessibilityCompatible(existingType, property.Symbol))
            {
                autoDetailInfo = new AutoDetailsInformation(
                    DiagnosticInfo.Create(
                        AnalyzerDiagnostics.AutoDetailsTypeAccessibilityMismatch,
                        GetPropertyLocation(property),
                        existingType.Name,
                        property.Name),
                    property.Name);
                return true;
            }

            targetNamespace = existingType.ContainingNamespace.IsGlobalNamespace
                ? null
                : existingType.ContainingNamespace.ToDisplayString();
        }
        else
        {
            // Tipo novo: gerado no namespace do tipo que declara a propriedade.
            targetNamespace = property.Symbol?.ContainingNamespace?.ToDisplayString();
        }

        // Descriptor novo para o tipo gerado; o descriptor da propriedade não é mutado.
        // O nome usa o tipo subjacente (sem a anotação '?') e o símbolo efetivo (sem o wrapper Nullable).
        var generatedType = new TypeDescriptor(
            declaredType.UnderlyingType,
            targetNamespace is not null ? [targetNamespace] : declaredType.Namespaces,
            effectiveSymbol);

        // Duas propriedades [AutoDetails] não podem gerar o mesmo tipo.
        var generatedTypeKey = $"{generatedType.Namespaces.FirstOrDefault()}.{generatedType.Name}";
        if (!generatedTypeKeys.Add(generatedTypeKey))
        {
            autoDetailInfo = new AutoDetailsInformation(
                DiagnosticInfo.Create(
                    AnalyzerDiagnostics.DuplicatedAutoDetailsType,
                    GetPropertyLocation(property),
                    generatedType.Name,
                    property.Name),
                property.Name);
            return true;
        }

        // Cria as informações de propriedades automáticas.
        var autoPropertiesBuild = AutoPropertiesGenerator.CreateBuildInformation(generatedType, fromPropertyType, autoDetailsAttribute);

        // Define as propriedades conhecidas para o tipo gerado e para a correspondência da propriedade.
        generatedType.DefinedProperties = autoPropertiesBuild.Properties;
        declaredType.DefinedProperties = autoPropertiesBuild.Properties;

        // Cria as informações de AutoDetails.
        autoDetailInfo = new AutoDetailsInformation(
            generatedType.Name,
            autoPropertiesBuild.ToInformation());

        return true;
    }

    private static Location? GetPropertyLocation(PropertyDescriptor property)
    {
        return property.Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
            is PropertyDeclarationSyntax propertySyntax
                ? propertySyntax.Identifier.GetLocation()
                : property.Symbol?.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
    }

    private static bool IsPartialClassInSource(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
            return false;

        var declarations = type.DeclaringSyntaxReferences;
        if (declarations.Length == 0)
            return false;

        return declarations
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static bool IsAccessibilityCompatible(INamedTypeSymbol existingType, IPropertySymbol? propertySymbol)
    {
        if (propertySymbol is null)
            return true;

        // Exposição efetiva da propriedade: o menor nível entre a propriedade e a classe que a declara.
        // A comparação numérica do enum é uma aproximação suficiente para tipos de nível de namespace
        // (public/internal), que é o cenário do AutoDetails.
        var propertyAccessibility = propertySymbol.DeclaredAccessibility;
        var containingAccessibility = propertySymbol.ContainingType?.DeclaredAccessibility ?? Accessibility.Public;
        var requiredAccessibility = propertyAccessibility < containingAccessibility
            ? propertyAccessibility
            : containingAccessibility;

        return existingType.DeclaredAccessibility >= requiredAccessibility;
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

        var targetNamespace = originType.Declaration is { IsError: false } existingType
            ? string.IsNullOrWhiteSpace(existingType.NamespaceName)
                ? null
                : existingType.NamespaceName
            : originType.Namespaces.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(targetNamespace))
        {
            GenerateInGlobalNamespace(context, className, originType, properties);
            return;
        }

        // 1 - Criação da classe
        var detailsClass = new ClassGenerator(className, targetNamespace!);
        GeneratedSourceConventions.ApplyRequiredNamespaces(detailsClass);

        // Quando completa um tipo partial preexistente, o atributo vai nos membros;
        // quando a classe é gerada do zero, docs e [GeneratedCode] vão no tipo (DF11).
        var completesExistingType = originType.Declaration is { IsError: false };
        if (!completesExistingType)
        {
            detailsClass.Attributes.Add(new RawLinesGeneratorNode(
                "/// <summary>Generated details class, projected from the source type.</summary>",
                GeneratedSourceConventions.GeneratedCodeAttributeLine));
        }

        // 1.1 - Modificadores (usa a mesma acessibilidade do tipo de origem)
        GeneratedSourceConventions.ApplyDeclaredAccessibility(detailsClass, originType.Declaration);

        // 1.2 - partial, para o desenvolvedor poder estender
        detailsClass.Modifiers.Partial();

        // 2 - Criação das propriedades públicas com get/set
        foreach (var p in properties)
        {
            detailsClass.Properties.Add(CreatePropertyGenerator(p, completesExistingType));
        }

        // 3 - Configura o nome do arquivo e gera
        detailsClass.FileName = GeneratedSourceConventions.FileName(
            originType,
            className,
            "AutoDetails");
        detailsClass.Generate(context);
    }

    private static AnnotatedPropertyGenerator CreatePropertyGenerator(
        PropertySnapshot property,
        bool completesExistingType)
    {
        var propertyType = GeneratedSourceConventions.ToTypeDescriptor(property.Type);

        string[] prefixLines = completesExistingType
            ?
            [
                "/// <summary>Generated property, projected from the source type.</summary>",
                GeneratedSourceConventions.GeneratedCodeAttributeLine,
            ]
            : ["/// <summary>Generated property, projected from the source type.</summary>"];

        var generator = new AnnotatedPropertyGenerator(propertyType, property.Name, prefixLines);
        generator.Modifiers.Public();
        return generator;
    }

    private static void GenerateInGlobalNamespace(
        SourceProductionContext context,
        string className,
        TypeSnapshot originType,
        IReadOnlyList<PropertySnapshot> properties)
    {
        // ClassGenerator sempre emite uma declaração de namespace. Para completar um tipo
        // global, emitimos a pequena declaração partial diretamente, preservando as mesmas
        // convenções de cabeçalho, docs e [GeneratedCode] usadas no caminho normal.
        var propertyGenerators = properties
            .Select(property => CreatePropertyGenerator(property, completesExistingType: true))
            .ToList();

        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
        };
        foreach (var property in properties)
        foreach (var ns in property.Type.Namespaces)
        {
            if (!string.IsNullOrWhiteSpace(ns))
                namespaces.Add(ns);
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable annotations");
        builder.AppendLine();
        foreach (var ns in namespaces.OrderBy(static value => value, StringComparer.Ordinal))
            builder.Append("using ").Append(ns).AppendLine(";");

        builder.AppendLine();
        builder.Append(originType.Declaration?.Accessibility ?? "public")
            .Append(" partial class ").AppendLine(className)
            .Append('{');
        foreach (var property in propertyGenerators)
            property.Write(builder, 1);
        builder.AppendLine("}");

        context.AddSource(
            GeneratedSourceConventions.FileName(originType, className, "AutoDetails"),
            builder.ToString());
    }

}
