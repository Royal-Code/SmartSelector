using Microsoft.CodeAnalysis;
using RoyalCode.SmartSelector.Generators.Generators;

namespace RoyalCode.SmartSelector.Generators;

/// <summary>
/// Incremental source generator that generates code for the AutoSelectAttribute{TFrom} attribute.
/// </summary>
[Generator]
public class IncrementalGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipelineProperties = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AutoPropertiesGenerator.AutoPropertiesAttributeTypedFullName,
            predicate: GeneratorSyntaxPredicates.IsClass,
            transform: AutoPropertiesGenerator.Transform);

        var pipelineNonGenericProperties = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AutoPropertiesGenerator.AutoPropertiesAttributeFullName,
            predicate: GeneratorSyntaxPredicates.IsClass,
            transform: AutoPropertiesGenerator.ValidateNonGenericUsage);

        var pipelineSelect = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AutoSelectGenerator.AutoSelectAttributeFullName,
            predicate: GeneratorSyntaxPredicates.IsClass,
            transform: AutoSelectGenerator.Transform);

        context.RegisterSourceOutput(pipelineProperties, static (context, model) =>
        {
            model.Generate(context);
        });

        context.RegisterSourceOutput(pipelineNonGenericProperties, static (context, diagnostic) =>
        {
            if (diagnostic is not null)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        context.RegisterSourceOutput(pipelineSelect, static (context, model) =>
        {
            model.Generate(context);
        });
    }
}
