using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

[AutoSelect<Product>]
public partial class ProductDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

[AutoSelect<Product>]
public partial class CustomProductDetails
{
    [MapFrom("Id")]
    public int CustomId { get; set; }
    [MapFrom(nameof(Product.Name))]
    public string CustomName { get; set; } = default!;
    [MapFrom(nameof(Product.IsActive))]
    public bool CustomIsActive { get; set; }
}