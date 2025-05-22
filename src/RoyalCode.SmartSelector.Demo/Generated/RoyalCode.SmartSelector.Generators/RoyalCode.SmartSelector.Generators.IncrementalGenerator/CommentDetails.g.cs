using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public partial class CommentDetails
{
    private static Func<Comment, CommentDetails> selectCommentFunc;

    public static Expression<Func<Comment, CommentDetails>> SelectCommentExpression { get; } = a => new CommentDetails
    {
        Content = a.Content,
        AuthorName = a.Author.Name
    };

    public static CommentDetails From(Comment comment) => (selectCommentFunc ??= SelectCommentExpression.Compile())(comment);
}
