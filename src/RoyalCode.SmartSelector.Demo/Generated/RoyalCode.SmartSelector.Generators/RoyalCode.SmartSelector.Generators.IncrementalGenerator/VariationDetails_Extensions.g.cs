using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

public static class VariationDetails_Extensions
{
    public static IQueryable<VariationDetails> SelectVariationDetails(this IQueryable<Variation> query)
    {
        return query.Select(VariationDetails.SelectVariationExpression);
    }

    public static IEnumerable<VariationDetails> SelectVariationDetails(this IEnumerable<Variation> enumerable)
    {
        return enumerable.Select(VariationDetails.From);
    }

    public static VariationDetails ToVariationDetails(this Variation variation) => VariationDetails.From(variation);
}
