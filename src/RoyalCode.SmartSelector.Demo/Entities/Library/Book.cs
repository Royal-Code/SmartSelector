namespace RoyalCode.SmartSelector.Demo.Entities.Library;

public class Book : Entity<Guid>
{
    public required string Title { get; set; }
    
    public required string Author { get; set; }
    
    public DateTime PublishedDate { get; set; }
    
    public required string ISBN { get; set; }
    
    public decimal Price { get; set; }
    
    public bool InStock { get; set; }

    public string? Sku { get; set; }

    public required Shelf Shelf { get; set; }
}
