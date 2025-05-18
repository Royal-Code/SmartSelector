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
}
