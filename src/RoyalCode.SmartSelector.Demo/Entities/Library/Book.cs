namespace RoyalCode.SmartSelector.Demo.Entities.Library;

public class Book : Entity<Guid>
{
    public string Title { get; set; }
    
    public string Author { get; set; }
    
    public DateTime PublishedDate { get; set; }
    
    public string ISBN { get; set; }
    
    public decimal Price { get; set; }
    
    public bool InStock { get; set; }

    public string? Sku { get; set; }

    public Shelf Shelf { get; set; }
}
