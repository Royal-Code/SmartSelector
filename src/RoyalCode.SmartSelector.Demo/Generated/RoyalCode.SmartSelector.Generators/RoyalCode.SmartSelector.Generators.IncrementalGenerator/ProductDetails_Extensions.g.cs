using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

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
