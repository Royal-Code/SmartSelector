namespace RoyalCode.SmartSelector.Demo.Entities.Library;

public class Shelf : Entity<Guid>
{
    public string Location { get; set; }
    
    public List<Book> Books { get; set; } = new();
}