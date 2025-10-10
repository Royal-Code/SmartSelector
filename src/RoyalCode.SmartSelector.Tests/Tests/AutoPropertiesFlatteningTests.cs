using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoPropertiesFlatteningTests
{
    [Fact]
    public void Should_Generate_Flattened_Properties_With_NonGeneric_AutoProperties()
    {
        // arrange + act
        Util.Compile(Source.NonGeneric, out var compilation, out var diagnostics);

        // assert - nenhum erro de geração
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generated = string.Join("\n-----\n", compilation.SyntaxTrees.Skip(1).Select(t => t.ToString()));

        // propriedades simples continuam
        generated.Should().Contain("public int Id { get; set; }");
        // propriedade complexa original NÃO deve ser criada automaticamente (foi flattening)
        generated.Should().NotContain("public Nested Nested { get; set; }");
        // propriedades flatten criadas
        generated.Should().Contain("public string NestedValue { get; set; }");
        generated.Should().Contain("public int NestedCount { get; set; }");
    }

    [Fact]
    public void Should_Generate_Flattened_Properties_With_Generic_AutoProperties()
    {
        // arrange + act
        Util.Compile(Source.Generic, out var compilation, out var diagnostics);

        // assert - nenhum erro de geração
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generated = string.Join("\n-----\n", compilation.SyntaxTrees.Skip(1).Select(t => t.ToString()));

        generated.Should().Contain("public int Id { get; set; }");
        generated.Should().NotContain("public Nested Nested { get; set; }");
        generated.Should().Contain("public string NestedValue { get; set; }");
        generated.Should().Contain("public int NestedCount { get; set; }");
    }
}

file static class Source
{
    public const string NonGeneric =
    """
using System; 
using RoyalCode.SmartSelector;

namespace Tests.SmartSelector.Models;

[AutoSelect<Origin>]
[AutoProperties(Flattening = new [] { nameof(Origin.Nested) })]
public partial class Dto { }

public class Origin
{
    public int Id { get; set; }
    public Nested Nested { get; set; } = new();
}

public class Nested
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}
""";

    public const string Generic =
    """
using System; 
using RoyalCode.SmartSelector;

namespace Tests.SmartSelector.Models;

[AutoProperties<Origin>(Flattening = new [] { nameof(Origin.Nested) })]
public partial class Dto { }

public class Origin
{
    public int Id { get; set; }
    public Nested Nested { get; set; } = new();
}

public class Nested
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}
""";
}
