using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

[AutoSelect<Product>]
public partial class ProductDetails
{
    public int Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
}
