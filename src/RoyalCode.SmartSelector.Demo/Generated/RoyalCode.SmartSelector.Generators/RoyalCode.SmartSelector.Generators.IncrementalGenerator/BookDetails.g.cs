using RoyalCode.SmartSelector.Demo.Entities.Library;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details.Library;

public partial class BookDetails
{
    private static Func<Book, BookDetails> selectBookFunc;

    public static Expression<Func<Book, BookDetails>> SelectBookExpression { get; } = a => new BookDetails
    {
        Id = a.Id,
        Title = a.Title,
        Author = a.Author,
        PublishedDate = a.PublishedDate,
        ISBN = a.ISBN,
        Price = a.Price,
        InStock = a.InStock
    };

    public static BookDetails From(Book book) => (selectBookFunc ??= SelectBookExpression.Compile())(book);
}
