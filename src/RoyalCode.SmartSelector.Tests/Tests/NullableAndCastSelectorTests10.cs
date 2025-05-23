using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_Nulls_ValueSelect()
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
using System.Collections.Generic;

namespace Tests.SmartSelector.Models;

public class ValueObject
{
    public string Value { get; set; } = default!;
}

public class ValueSelectNulls
{
    public ICollection<ValueObject> Value1 { get; set; } = default!;
    public ICollection<ValueObject> Value2 { get; set; } = default!;
    public ICollection<ValueObject>? Value3 { get; set; }
    public ICollection<ValueObject>? Value4 { get; set; }
}

public class ValueDto
{
    public string Value { get; set; } = default!;
}

[AutoSelect<ValueSelectNulls>]
public partial class ValueSelectNullsDto
{
    public IReadOnlyList<ValueDto> Value1 { get; set; } = default!;
    public IReadOnlyList<ValueDto>? Value2 { get; set; }
    public IReadOnlyList<ValueDto> Value3 { get; set; } = default!;
    public IReadOnlyList<ValueDto>? Value4 { get; set; }
}

""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class ValueSelectNullsDto
{
    private static Func<ValueSelectNulls, ValueSelectNullsDto> selectValueSelectNullsFunc;

    public static Expression<Func<ValueSelectNulls, ValueSelectNullsDto>> SelectValueSelectNullsExpression { get; } = a => new ValueSelectNullsDto
    {
        Value1 = a.Value1.Select(b => new ValueDto
        {
            Value = b.Value
        }).ToList(),
        Value2 = a.Value2.Select(b => new ValueDto
        {
            Value = b.Value
        }).ToList(),
        Value3 = a.Value3.Select(b => new ValueDto
        {
            Value = b.Value
        }).ToList(),
        Value4 = a.Value4.Select(b => new ValueDto
        {
            Value = b.Value
        }).ToList()
    };

    public static ValueSelectNullsDto From(ValueSelectNulls valueSelectNulls) => (selectValueSelectNullsFunc ??= SelectValueSelectNullsExpression.Compile())(valueSelectNulls);
}

""";
}