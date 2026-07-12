using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class MapFromTests
{
    [Fact]
    public void Select_ProductDetails_With_MapFromAttribute()
    {
        var result = Util.CompileAndAssert(Code.Types);

        var generatedInterface = result.GeneratedSource("ProductDetails.g.cs");
        generatedInterface.Should().Be(Code.ExpectedPartial);

        var generatedHandler = result.GeneratedSource("ProductDetails_Extensions.g.cs");
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
    [MapFrom("Id")]
    public Guid CustomId { get; set; }
    
    [MapFrom(nameof(Product.Name))]
    public string CustomName { get; set; }

    [MapFrom(nameof(Product.Active))]
    public bool CustomActive { get; set; }
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
        CustomId = a.Id,
        CustomName = a.Name,
        CustomActive = a.Active
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
