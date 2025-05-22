namespace RoyalCode.SmartSelector.Demo.Entities;

public class Variation : Entity<long>
{
    public Variation(Product product, string name, string code, Color color, Size size)
    {
        Id = Random.Shared.NextInt64(1, 1000);
        Product = product;
        Name = name;
        Code = code;
        Color = color;
        Size = size;
    }

#nullable disable
    // Parameterless constructor for EF Core
    protected Variation() { }
#nullable enable

    public Product Product { get; }

    public string Name { get; set; }

    public string Code { get; set; }

    public Color Color { get; set; }

    public Size Size { get; set; }

    public bool Active { get; set; } = true;
}
