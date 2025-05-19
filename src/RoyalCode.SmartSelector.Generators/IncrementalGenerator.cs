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
        var pipelineSelect = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: AutoSelectGenerator.AutoSelectAttributeFullName,
            predicate: AutoSelectGenerator.Predicate,
            transform: AutoSelectGenerator.Transform);

        context.RegisterSourceOutput(pipelineSelect.Collect(), static (context, models) =>
        {
            foreach(var model in models)
                model.Generate(context);
        });
    }
}
