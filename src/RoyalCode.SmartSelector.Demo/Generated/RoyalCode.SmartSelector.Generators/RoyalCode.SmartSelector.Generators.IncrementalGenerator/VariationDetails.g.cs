using RoyalCode.SmartSelector.Demo.Entities;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details;

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
