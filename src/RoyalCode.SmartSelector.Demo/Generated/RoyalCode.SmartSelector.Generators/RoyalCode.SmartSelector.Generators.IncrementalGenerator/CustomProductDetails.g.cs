using RoyalCode.SmartSelector.Demo.Entities;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details;

public partial class CustomProductDetails
{
    private static Func<Product, CustomProductDetails> selectProductFunc;

    public static Expression<Func<Product, CustomProductDetails>> SelectProductExpression { get; } = a => new CustomProductDetails
    {
        CustomId = a.Id,
        CustomName = a.Name,
        CustomIsActive = a.IsActive
    };

    public static CustomProductDetails From(Product product) => (selectProductFunc ??= SelectProductExpression.Compile())(product);
}
