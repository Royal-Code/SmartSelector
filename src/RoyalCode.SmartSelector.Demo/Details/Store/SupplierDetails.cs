using RoyalCode.SmartSelector.Demo.Entities.Store;

namespace RoyalCode.SmartSelector.Demo.Details.Store;

// Cenário da política de null (DF5/DF18):
// - Warehouse: navegação anulável para destino anulável -> condicional propaga null.
// - Contacts: coleção anulável para destino não anulável -> coleção vazia quando null (RCSS016, Info).
[AutoSelect<Supplier>, AutoProperties]
public partial class SupplierDetails
{
    [AutoDetails]
    public WarehouseDetails? Warehouse { get; set; }

    public IReadOnlyList<ContactDetails> Contacts { get; set; } = [];
}

public class ContactDetails
{
    public Guid Id { get; set; }

    public required string Email { get; set; }
}
