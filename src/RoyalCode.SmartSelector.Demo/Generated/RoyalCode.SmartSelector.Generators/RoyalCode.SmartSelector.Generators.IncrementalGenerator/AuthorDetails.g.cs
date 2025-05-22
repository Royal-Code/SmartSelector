using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public partial class AuthorDetails
{
    private static Func<Author, AuthorDetails> selectAuthorFunc;

    public static Expression<Func<Author, AuthorDetails>> SelectAuthorExpression { get; } = a => new AuthorDetails
    {
        Name = a.Name
    };

    public static AuthorDetails From(Author author) => (selectAuthorFunc ??= SelectAuthorExpression.Compile())(author);
}
