using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoyalCode.SmartSelector.Generators;

namespace RoyalCode.SmartSelector.Tests;

internal static class Util
{
    internal static void Compile(
        string sourceCode,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        // the source code to be compiled
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // assemblies references required to compile the source code
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Util).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ICollection<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IReadOnlyList<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AutoSelectAttribute<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MapFromAttribute).Assembly.Location),
        };

        // create a compilation for the source code.
        var compilation = CSharpCompilation.Create("SourceGeneratorTests", [syntaxTree], references, 
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // apply the source generator and collect the output
        var driver = CSharpGeneratorDriver.Create(new IncrementalGenerator());

        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);
    }
}
