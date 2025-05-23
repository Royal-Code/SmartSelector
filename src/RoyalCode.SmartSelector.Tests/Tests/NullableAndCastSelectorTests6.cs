using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_Nulls_Enums()
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

public enum NullsKind
{
    None,
    Nullable,
    NonNullable
}

public class EnumNulls
{
    public NullsKind Value1 { get; set; } = default!;
    public NullsKind Value2 { get; set; } = default!;
    public NullsKind? Value3 { get; set; }
    public NullsKind? Value4 { get; set; }
}

[AutoSelect<EnumNulls>]
public partial class EnumNullsDto
{
    public NullsKind Value1 { get; set; } = default!;
    public NullsKind? Value2 { get; set; }
    public NullsKind Value3 { get; set; } = default!;
    public NullsKind? Value4 { get; set; }
}

""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class EnumNullsDto
{
    private static Func<EnumNulls, EnumNullsDto> selectEnumNullsFunc;

    public static Expression<Func<EnumNulls, EnumNullsDto>> SelectEnumNullsExpression { get; } = a => new EnumNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3.HasValue ? a.Value3.Value : default,
        Value4 = a.Value4
    };

    public static EnumNullsDto From(EnumNulls enumNulls) => (selectEnumNullsFunc ??= SelectEnumNullsExpression.Compile())(enumNulls);
}

""";
}