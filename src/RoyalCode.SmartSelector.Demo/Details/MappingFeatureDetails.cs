using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Entities.Store;

namespace RoyalCode.SmartSelector.Demo.Details;

[AutoSelect<Product>(Exclude = [nameof(Product.UpdatedAt)])]
public partial class AutoConfiguredProductDetails
{
}

[AutoSelect<Supplier>]
public partial class SupplierWarehouseLocationDetails
{
    [MapFrom("Warehouse.Location")]
    public string? Location { get; set; }
}
