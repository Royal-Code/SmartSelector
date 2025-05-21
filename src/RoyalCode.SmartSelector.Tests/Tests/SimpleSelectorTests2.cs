using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class SimpleSelectorTests
{
    [Fact]
    public void Direct_Select_VariationDetails()
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

#nullable disable // poco

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}

public class Color : Entity<long>
{
    public Color(string name, string hexCode)
    {
        Name = name;
        HexCode = hexCode;
    }
    public string Name { get; set; }

    public string HexCode { get; set; }
}

public class Size : Entity<long>
{
    public Size(string name, string code)
    {
        Name = name;
        Code = code;
    }

    public string Name { get; set; }

    public string Code { get; set; }
}

public class Variation : Entity<long>
{
    public Variation(Product product, string name, string code, Color color, Size size)
    {
        Product = product;
        Name = name;
        Code = code;
        Color = color;
        Size = size;
    }

    public Product Product { get; }

    public string Name { get; set; }

    public string Code { get; set; }

    public Color Color { get; set; }

    public Size Size { get; set; }

    public bool Active { get; set; } = true;
}

[AutoSelect<Variation>]
public partial class VariationDetails
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public string ColorName { get; set; }

    public string SizeCode { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class VariationDetails
{
    private static Func<Variation, VariationDetails> selectVariationFunc;

    public static Expression<Func<Variation, VariationDetails>> SelectVariationExpression { get; } = a => new VariationDetails
    {
        Id = a.Id,
        Name = a.Name,
        Code = a.Code,
        ColorName = a.Color.Name,
        SizeCode = a.Size.Code
    };

    public static VariationDetails From(Variation variation) => (selectVariationFunc ??= SelectVariationExpression.Compile())(variation);
}

""";
}