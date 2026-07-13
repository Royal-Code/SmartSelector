using System.Collections;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RoyalCode.SmartSelector.Generators;
using RoyalCode.SmartSelector.Generators.Generators;
using RoyalCode.SmartSelector.Generators.Models;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class IncrementalPipelineTests
{
    private const string Fixture =
        """
        using RoyalCode.SmartSelector;

        namespace Incremental;

        public class Address { public string? City { get; set; } }
        public class Entity { public int Id { get; set; } public Address? Address { get; set; } }

        [AutoSelect<Entity>, AutoProperties]
        public partial class Details
        {
            public string? AddressCity { get; set; }

            [AutoDetails]
            public AddressDetails? Address { get; set; }
        }
        """;

    [Fact]
    public void Irrelevant_edit_should_keep_source_outputs_cached_or_unchanged()
    {
        var compilation = Util.CreateIncrementalTestCompilation(Fixture, out var consumerTree);
        var driver = CreateDriver();
        driver = driver.RunGenerators(compilation);

        var changedTree = consumerTree.WithChangedText(SourceText.From(Fixture + "\n// irrelevant edit"));
        compilation = compilation.ReplaceSyntaxTree(consumerTree, changedTree);
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var sourceOutputs = result.TrackedSteps
            .Where(pair => pair.Key.Contains("SourceOutput", StringComparison.Ordinal))
            .SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs)
            .ToArray();

        Assert.NotEmpty(sourceOutputs);
        Assert.All(sourceOutputs, output =>
            Assert.Contains(output.Reason, new[]
            {
                IncrementalStepRunReason.Cached,
                IncrementalStepRunReason.Unchanged,
            }));
    }

    [Fact]
    public void Retained_models_should_not_reach_Roslyn_compilation_objects()
    {
        var compilation = Util.CreateIncrementalTestCompilation(Fixture, out _);
        var driver = CreateDriver().RunGenerators(compilation);
        var retainedModels = driver.GetRunResult().Results.Single().TrackedSteps
            .SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Value)
            .Where(value => value is AutoSelectInformation or AutoPropertiesInformation or AutoDetailsInformation or DiagnosticInfo)
            .ToArray();

        Assert.NotEmpty(retainedModels);
        foreach (var model in retainedModels)
        foreach (var value in Traverse(model))
        {
            Assert.False(
                value is ISymbol or SyntaxNode or SyntaxTree or SemanticModel or Compilation or Diagnostic or Location,
                $"Retained model reaches forbidden Roslyn object '{value.GetType().FullName}'.");
        }
    }

    private static GeneratorDriver CreateDriver()
    {
        var options = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        return CSharpGeneratorDriver.Create(
            generators: [new IncrementalGenerator().AsSourceGenerator()],
            parseOptions: new CSharpParseOptions(LanguageVersion.Preview),
            driverOptions: options);
    }

    private static IEnumerable<object> Traverse(object root)
    {
        var queue = new Queue<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var value = queue.Dequeue();
            if (!visited.Add(value))
                continue;
            yield return value;

            var type = value.GetType();
            if (value is string || type.IsPrimitive || type.IsEnum || type.IsPointer)
                continue;
            if (value is IEnumerable enumerable)
                foreach (var item in enumerable)
                    if (item is not null) queue.Enqueue(item);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (field.GetValue(value) is { } child) queue.Enqueue(child);
        }
    }
}
