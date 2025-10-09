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
        messageFormat: "Invalid use of AutoPropertyAttribute<TFrom> in class {0}, when using AutoSelectAttribute, use the non-generic version AutoPropertyAttribute",
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
}
