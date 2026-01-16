using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

public static class CustomProductDetails_Extensions
{
    public static IQueryable<CustomProductDetails> SelectCustomProductDetails(this IQueryable<Product> query)
    {
        return query.Select(CustomProductDetails.SelectProductExpression);
    }

    public static IEnumerable<CustomProductDetails> SelectCustomProductDetails(this IEnumerable<Product> enumerable)
    {
        return enumerable.Select(CustomProductDetails.From);
    }

    public static CustomProductDetails ToCustomProductDetails(this Product product) => CustomProductDetails.From(product);
}
