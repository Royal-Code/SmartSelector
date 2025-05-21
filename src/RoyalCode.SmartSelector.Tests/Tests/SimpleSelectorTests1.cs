using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class SimpleSelectorTests
{
    [Fact]
    public void Direct_Select_ProductDetails()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);

        var generatedHandler = output.SyntaxTrees.Skip(2).FirstOrDefault()?.ToString();
        generatedHandler.Should().Be(Code.ExpectedExtension);
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

public class Product : Entity<Guid>
{
    public Product(string name)
    {
        Name = name;
        Active = true;
    }
    public string Name { get; set; }
    public bool Active { get; set; }
}

[AutoSelect<Product>]
public partial class ProductDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class ProductDetails
{
    private static Func<Product, ProductDetails> selectProductFunc;

    public static Expression<Func<Product, ProductDetails>> SelectProductExpression { get; } = a => new ProductDetails
    {
        Id = a.Id,
        Name = a.Name,
        Active = a.Active
    };

    public static ProductDetails From(Product product) => (selectProductFunc ??= SelectProductExpression.Compile())(product);
}

""";

    public const string ExpectedExtension =
"""

namespace Tests.SmartSelector.Models;

public static class ProductDetails_Extensions
{
    public static IQueryable<ProductDetails> SelectProductDetails(this IQueryable<Product> query)
    {
        return query.Select(ProductDetails.SelectProductExpression);
    }

    public static IEnumerable<ProductDetails> SelectProductDetails(this IEnumerable<Product> enumerable)
    {
        return enumerable.Select(ProductDetails.From);
    }

    public static ProductDetails ToProductDetails(this Product product) => ProductDetails.From(product);
}

""";
}