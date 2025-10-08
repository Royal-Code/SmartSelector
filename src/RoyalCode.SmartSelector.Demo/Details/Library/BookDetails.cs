using RoyalCode.SmartSelector.Demo.Entities.Library;

namespace RoyalCode.SmartSelector.Demo.Details.Library;

[AutoSelect<Book>, AutoProperties(Exclude = [ nameof(Book.Sku) ])]
public partial class BookDetails
{
    public Guid Id { get; set; }
}

