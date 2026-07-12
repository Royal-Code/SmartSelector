using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators;

internal static class AnalyzerDiagnostics
{
    private const string Category = "Usage";

    public static readonly DiagnosticDescriptor InvalidAutoSelectType = new(
        id: "RCSS000",
        title: "Invalid Auto Select type",
        messageFormat: "Invalid use of AutoSelectAttribute: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotMatch = new(
        id: "RCSS001",
        title: "It is not possible to determine a corresponding property for the other type",
        messageFormat: "The property '{0}' does not match another property of the other type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotCompatible = new(
        id: "RCSS002",
        title: "Incompatible property types",
        messageFormat: "Incompatible property types, the property {0} of the type {1} is not compatible with the property {2} of the type {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAutoProperty = new(
        id: "RCSS003",
        title: "Invalid Auto Property attribute usage",
        messageFormat: "Invalid use of AutoPropertiesAttribute<TFrom> in class {0}, when using AutoSelectAttribute, use the non-generic version AutoPropertiesAttribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingAutoPropertiesAttributes = new(
        id: "RCSS004",
        title: "Conflicting AutoProperties attributes",
        messageFormat: "The class {0} cannot use both AutoPropertiesAttribute and AutoPropertiesAttribute<TFrom>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAutoPropertiesTypeArgument = new(
        id: "RCSS005",
        title: "Invalid AutoProperties type argument",
        messageFormat: "Invalid type argument '{0}' for AutoPropertiesAttribute<TFrom>; it must be a valid class or struct type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoPropertiesRequiresPartialClass = new(
        id: "RCSS006",
        title: "AutoProperties requires a partial class",
        messageFormat: "The class '{0}' must be partial to use AutoPropertiesAttribute<TFrom>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AutoPropertiesRequiresAutoSelect = new(
        id: "RCSS007",
        title: "AutoProperties requires AutoSelect",
        messageFormat: "AutoPropertiesAttribute requires AutoSelectAttribute<TFrom> on class '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericDestinationTypeNotSupported = new(
        id: "RCSS008",
        title: "Generic destination DTOs are not supported",
        messageFormat: "The destination DTO '{0}' cannot be generic; generic DTO support is not available",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedDestinationTypeNotSupported = new(
        id: "RCSS009",
        title: "Nested destination DTOs are not supported",
        messageFormat: "The destination DTO '{0}' cannot be nested; nested DTO support is not available",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousFlattening = new(
        id: "RCSS010",
        title: "Ambiguous flattened property path",
        messageFormat: "The property '{0}' matches multiple flattened paths in the source type; rename it or use MapFromAttribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GlobalNamespaceNotSupported = new(
        id: "RCSS011",
        title: "Destination DTOs in the global namespace are not supported",
        messageFormat: "The destination DTO '{0}' must be declared inside a namespace",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QualifiedAutoPropertiesNotSupported = new(
        id: "RCSS012",
        title: "Qualified AutoProperties syntax is not supported",
        messageFormat: "Qualified or aliased AutoPropertiesAttribute<TFrom> syntax is not supported yet; use the simple attribute name",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
