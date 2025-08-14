namespace RoyalCode.SmartSelector.Benchmarks.Models;

public class Product : Entity<int>
{
    public Product(string name)
    {
        Id = Random.Shared.Next(1, 10000);
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
    
    public string Name { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
