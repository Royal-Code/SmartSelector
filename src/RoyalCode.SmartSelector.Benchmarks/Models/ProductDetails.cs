namespace RoyalCode.SmartSelector.Benchmarks.Models;

[AutoSelect<Product>]
public partial class ProductDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
