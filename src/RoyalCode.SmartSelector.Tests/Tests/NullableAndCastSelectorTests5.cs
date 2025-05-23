using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_Nulls_Ints()
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

public class IntNulls
{
    public int Value1 { get; set; } = default!;
    public int Value2 { get; set; } = default!;
    public int? Value3 { get; set; }
    public int? Value4 { get; set; }
}

[AutoSelect<IntNulls>]
public partial class IntNullsDto
{
    public int Value1 { get; set; } = default!;
    public int? Value2 { get; set; }
    public int Value3 { get; set; } = default!;
    public int? Value4 { get; set; }
}

""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class IntNullsDto
{
    private static Func<IntNulls, IntNullsDto> selectIntNullsFunc;

    public static Expression<Func<IntNulls, IntNullsDto>> SelectIntNullsExpression { get; } = a => new IntNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3.HasValue ? a.Value3.Value : default,
        Value4 = a.Value4
    };

    public static IntNullsDto From(IntNulls intNulls) => (selectIntNullsFunc ??= SelectIntNullsExpression.Compile())(intNulls);
}

""";
}