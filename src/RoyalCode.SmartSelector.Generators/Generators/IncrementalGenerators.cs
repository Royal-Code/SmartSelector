using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

/// <summary>
/// Incremental source generator that generates code for the AutoSelectAttribute{TFrom} attribute.
/// </summary>
[Generator]
public class IncrementalGenerators : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipelineSelect = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AutoSelectGenerator.AutoSelectAttributeFullName,
            predicate: AutoSelectGenerator.Predicate,
            transform: AutoSelectGenerator.Transform);

        context.RegisterSourceOutput(pipelineSelect, static (context, model) =>
        {
            model.Generate(context);
        });
    }
}
