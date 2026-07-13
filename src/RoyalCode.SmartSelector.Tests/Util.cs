extern alias net80;
extern alias net90;
extern alias net100;

using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoyalCode.SmartSelector.Generators;

namespace RoyalCode.SmartSelector.Tests;

internal enum TestTargetFramework
{
    Net80,
    Net90,
    Net100,
}

internal sealed record CompileResult(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationDiagnostics,
    ImmutableDictionary<string, string> GeneratedSources,
    GeneratorDriverRunResult RunResult,
    Compilation OutputCompilation)
{
    internal IEnumerable<Diagnostic> Errors => GeneratorDiagnostics
        .Concat(CompilationDiagnostics)
        .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    internal IEnumerable<Diagnostic> Warnings => GeneratorDiagnostics
        .Concat(CompilationDiagnostics)
        .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);

    internal string GeneratedSource(string hintName) => GeneratedSources[hintName];

    internal string AllGeneratedSources() => string.Join(
        "\n-----\n",
        GeneratedSources.OrderBy(static pair => pair.Key).Select(static pair => pair.Value));
}

internal static class Util
{
    private static readonly string[] RuntimeSourceResourceNames =
    [
        "RuntimeSources.AutoSelectAttribute.cs",
        "RuntimeSources.AutoPropertiesAttribute.cs",
        "RuntimeSources.AutoDetailsAttribute.cs",
        "RuntimeSources.MapFromAttribute.cs",
    ];

    private const string ImplicitUsings =
        "global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;";

    internal static CompileResult CompileAndAssert(
        string sourceCode,
        bool assertNoWarnings = false,
        bool includeImplicitUsings = true)
    {
        CompileResult? snapshotResult = null;

        foreach (var targetFramework in Enum.GetValues<TestTargetFramework>())
        {
            var result = Compile(sourceCode, targetFramework, includeImplicitUsings);
            result.Errors.Should().BeEmpty(
                "generated code must compile for {0}; diagnostics:\n{1}",
                targetFramework,
                FormatDiagnostics(result));

            if (assertNoWarnings)
            {
                result.Warnings.Should().BeEmpty(
                    "generated code must not warn for {0}; diagnostics:\n{1}",
                    targetFramework,
                    FormatDiagnostics(result));
            }

            snapshotResult = result;
        }

        return snapshotResult!;
    }

    internal static CompileResult Compile(
        string sourceCode,
        TestTargetFramework targetFramework = TestTargetFramework.Net100,
        bool includeImplicitUsings = true) =>
        CompileCore(sourceCode, GetReferenceAssemblies(targetFramework), includeImplicitUsings);

    /// <summary>
    /// Fast path for tests that intentionally need the test host runtime rather than a supported TFM contract.
    /// Generation tests should use <see cref="CompileAndAssert"/>.
    /// </summary>
    internal static CompileResult CompileFast(string sourceCode)
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("The test host did not provide trusted platform assemblies.");

        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));

        return CompileCore(sourceCode, references, includeImplicitUsings: true);
    }

    internal static CSharpCompilation CreateIncrementalTestCompilation(
        string sourceCode,
        out SyntaxTree consumerTree)
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("The test host did not provide trusted platform assemblies.");
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        consumerTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions, "Consumer.cs");
        var runtimeTrees = RuntimeSourceResourceNames.Select(resourceName =>
            CSharpSyntaxTree.ParseText(
                $"using System;{Environment.NewLine}{ReadEmbeddedSource(resourceName)}",
                parseOptions,
                resourceName));
        return CSharpCompilation.Create(
            "IncrementalPipelineTests",
            runtimeTrees.Prepend(CSharpSyntaxTree.ParseText(ImplicitUsings, parseOptions)).Append(consumerTree),
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static CompileResult CompileCore(
        string sourceCode,
        IEnumerable<MetadataReference> frameworkReferences,
        bool includeImplicitUsings)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions);
        var runtimeSyntaxTrees = RuntimeSourceResourceNames.Select(resourceName =>
            CSharpSyntaxTree.ParseText(
                $"using System;{Environment.NewLine}{ReadEmbeddedSource(resourceName)}",
                parseOptions,
                resourceName));
        var syntaxTrees = runtimeSyntaxTrees.Append(syntaxTree);
        if (includeImplicitUsings)
        {
            syntaxTrees = syntaxTrees.Prepend(CSharpSyntaxTree.ParseText(ImplicitUsings, parseOptions));
        }

        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTests",
            syntaxTrees,
            frameworkReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            generators: [new IncrementalGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .ToImmutableDictionary(
                static source => source.HintName,
                static source => source.SourceText.ToString(),
                StringComparer.Ordinal);

        return new CompileResult(
            generatorDiagnostics,
            outputCompilation.GetDiagnostics(),
            generatedSources,
            runResult,
            outputCompilation);
    }

    private static IEnumerable<MetadataReference> GetReferenceAssemblies(
        TestTargetFramework targetFramework) => targetFramework switch
        {
            TestTargetFramework.Net80 => net80::Basic.Reference.Assemblies.Net80.References.All,
            TestTargetFramework.Net90 => net90::Basic.Reference.Assemblies.Net90.References.All,
            TestTargetFramework.Net100 => net100::Basic.Reference.Assemblies.Net100.References.All,
            _ => throw new ArgumentOutOfRangeException(nameof(targetFramework), targetFramework, null),
        };

    private static string FormatDiagnostics(CompileResult result) => string.Join(
        Environment.NewLine,
        result.GeneratorDiagnostics.Concat(result.CompilationDiagnostics));

    private static string ReadEmbeddedSource(string resourceName)
    {
        using var stream = typeof(Util).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded runtime source '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
