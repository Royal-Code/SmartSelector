namespace RoyalCode.SmartSelector.Tests.Models.Expected;

#nullable disable // poco

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}

public class Product : Entity<Guid>
{
    public Product(string nome)
    {
        Name = nome;
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
