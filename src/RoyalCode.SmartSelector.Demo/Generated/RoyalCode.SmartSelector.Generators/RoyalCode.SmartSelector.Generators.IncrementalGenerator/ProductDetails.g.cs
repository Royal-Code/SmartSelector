using RoyalCode.SmartSelector.Demo.Entities;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details;

public partial class ProductDetails
{
    private static Func<Product, ProductDetails> selectProductFunc;

    public static Expression<Func<Product, ProductDetails>> SelectProductExpression { get; } = a => new ProductDetails
    {
        Id = a.Id,
        Name = a.Name,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    public static ProductDetails From(Product product) => (selectProductFunc ??= SelectProductExpression.Compile())(product);
}
