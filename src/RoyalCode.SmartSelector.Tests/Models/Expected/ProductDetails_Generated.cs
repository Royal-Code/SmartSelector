using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

// Generated code
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
