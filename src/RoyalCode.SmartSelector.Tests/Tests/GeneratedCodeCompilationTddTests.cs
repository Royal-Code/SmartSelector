using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoyalCode.SmartSelector.Generators;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Characterization tests for known generator limitations.
/// These tests intentionally remain red until each corresponding production fix is implemented.
/// </summary>
public class GeneratedCodeCompilationTddTests
{
    [Fact]
    public void Generated_code_should_compile_for_a_generic_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class EntityDetails<T>
            {
                public int Id { get; set; }
            }
            """);
    }

    [Fact]
    public void Generated_code_should_compile_for_a_nested_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            public class Entity { public int Id { get; set; } }

            public partial class Container
            {
                [AutoSelect<Entity>]
                public partial class EntityDetails
                {
                    public int Id { get; set; }
                }

                public static EntityDetails Map(Entity value) => EntityDetails.From(value);
            }
            """);
    }

    [Fact]
    public void Generated_code_should_compile_with_a_fully_qualified_AutoProperties_attribute()
    {
        AssertGeneratedCodeCompiles(
            """
            namespace Domain
            {
                public class Entity { public int Id { get; set; } }
            }

            namespace Dtos
            {
                [global::RoyalCode.SmartSelector.AutoProperties<Domain.Entity>]
                public partial class EntityDetails { }

                public static class Consumer
                {
                    public static int Read(EntityDetails value) => value.Id;
                }
            }
            """);
    }

    private static void AssertGeneratedCodeCompiles(string source)
    {
        CompileWithPlatformReferences(source, out var output, out var generatorDiagnostics);

        var errors = generatorDiagnostics
            .Concat(output.GetDiagnostics())
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.Empty(errors);
    }

    private static void CompileWithPlatformReferences(
        string source,
        out Compilation output,
        out IReadOnlyList<Diagnostic> generatorDiagnostics)
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("The test host did not provide trusted platform assemblies.");

        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(AutoSelectAttribute<>).Assembly.Location));

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            "GeneratedCodeCompilationTddTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            generators: [new IncrementalGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out output,
            out var diagnostics);

        generatorDiagnostics = diagnostics;
    }
}
