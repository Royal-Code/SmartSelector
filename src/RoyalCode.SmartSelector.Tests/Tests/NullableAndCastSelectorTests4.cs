using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_Nulls_Strings()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);
    }
}

file static class Code
{
    public const string Types =
"""
using RoyalCode.SmartSelector;

namespace Tests.SmartSelector.Models;

public class StringNulls
{
    public string Value1 { get; set; } = default!;
    public string Value2 { get; set; } = default!;
    public string? Value3 { get; set; }
    public string? Value4 { get; set; }
}

[AutoSelect<StringNulls>]
public partial class StringNullsDto
{
    public string Value1 { get; set; } = default!;
    public string? Value2 { get; set; }
    public string Value3 { get; set; } = default!;
    public string? Value4 { get; set; }
}

""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class StringNullsDto
{
    private static Func<StringNulls, StringNullsDto> selectStringNullsFunc;

    public static Expression<Func<StringNulls, StringNullsDto>> SelectStringNullsExpression { get; } = a => new StringNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3,
        Value4 = a.Value4
    };

    public static StringNullsDto From(StringNulls stringNulls) => (selectStringNullsFunc ??= SelectStringNullsExpression.Compile())(stringNulls);
}

""";
}