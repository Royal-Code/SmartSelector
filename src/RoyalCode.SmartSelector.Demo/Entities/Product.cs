namespace RoyalCode.SmartSelector.Demo.Entities;

public class Product : Entity<int>
{
    public Product(string name)
    {
        Id = Random.Shared.Next(1, 10000);
        Name = name;
        IsActive = true;
    }
    
    public string Name { get; set; }

    public bool IsActive { get; set; }
}
