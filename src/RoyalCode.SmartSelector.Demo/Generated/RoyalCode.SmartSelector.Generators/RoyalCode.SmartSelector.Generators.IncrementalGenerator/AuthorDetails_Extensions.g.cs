using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public static class AuthorDetails_Extensions
{
    public static IQueryable<AuthorDetails> SelectAuthorDetails(this IQueryable<Author> query)
    {
        return query.Select(AuthorDetails.SelectAuthorExpression);
    }

    public static IEnumerable<AuthorDetails> SelectAuthorDetails(this IEnumerable<Author> enumerable)
    {
        return enumerable.Select(AuthorDetails.From);
    }

    public static AuthorDetails ToAuthorDetails(this Author author) => AuthorDetails.From(author);
}
