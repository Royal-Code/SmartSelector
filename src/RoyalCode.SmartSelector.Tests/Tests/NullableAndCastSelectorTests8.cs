using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_Nulls_Enumerables()
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

public class EnumerableNulls
{
    public IEnumerable<string> Value1 { get; set; } = default!;
    public IEnumerable<string> Value2 { get; set; } = default!;
    public IEnumerable<string>? Value3 { get; set; }
    public IEnumerable<string>? Value4 { get; set; }
}

[AutoSelect<EnumerableNulls>]
public partial class EnumerableNullsDto
{
    public IEnumerable<string> Value1 { get; set; } = default!;
    public IEnumerable<string>? Value2 { get; set; }
    public IEnumerable<string> Value3 { get; set; } = default!;
    public IEnumerable<string>? Value4 { get; set; }
}

""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class EnumerableNullsDto
{
    private static Func<EnumerableNulls, EnumerableNullsDto> selectEnumerableNullsFunc;

    public static Expression<Func<EnumerableNulls, EnumerableNullsDto>> SelectEnumerableNullsExpression { get; } = a => new EnumerableNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3,
        Value4 = a.Value4
    };

    public static EnumerableNullsDto From(EnumerableNulls enumerableNulls) => (selectEnumerableNullsFunc ??= SelectEnumerableNullsExpression.Compile())(enumerableNulls);
}

""";
}