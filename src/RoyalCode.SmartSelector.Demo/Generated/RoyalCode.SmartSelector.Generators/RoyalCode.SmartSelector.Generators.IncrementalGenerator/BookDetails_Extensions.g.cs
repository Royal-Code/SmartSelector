using RoyalCode.SmartSelector.Demo.Entities.Library;

namespace RoyalCode.SmartSelector.Demo.Details.Library;

public static class BookDetails_Extensions
{
    public static IQueryable<BookDetails> SelectBookDetails(this IQueryable<Book> query)
    {
        return query.Select(BookDetails.SelectBookExpression);
    }

    public static IEnumerable<BookDetails> SelectBookDetails(this IEnumerable<Book> enumerable)
    {
        return enumerable.Select(BookDetails.From);
    }

    public static BookDetails ToBookDetails(this Book book) => BookDetails.From(book);
}
