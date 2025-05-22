using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

#nullable disable

[AutoSelect<Variation>]
public partial class VariationDetails
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public string ColorName { get; set; }

    public string SizeCode { get; set; }
}
