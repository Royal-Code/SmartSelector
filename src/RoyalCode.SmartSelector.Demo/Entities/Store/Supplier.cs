namespace RoyalCode.SmartSelector.Demo.Entities.Store;

public class Supplier : Entity<Guid>
{
    public required string Name { get; set; }

    public Warehouse? Warehouse { get; set; }

    public ICollection<Contact>? Contacts { get; set; }
}
