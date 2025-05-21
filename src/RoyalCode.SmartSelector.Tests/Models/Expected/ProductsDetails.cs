namespace RoyalCode.SmartSelector.Tests.Models.Expected;

#nullable disable // poco

public class Product : Entity<Guid>
{
    public Product(string name)
    {
        Name = name;
        Active = true;
    }
    public string Name { get; set; }
    public bool Active { get; set; }
}

[AutoSelect<Product>]
public partial class ProductDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
}
