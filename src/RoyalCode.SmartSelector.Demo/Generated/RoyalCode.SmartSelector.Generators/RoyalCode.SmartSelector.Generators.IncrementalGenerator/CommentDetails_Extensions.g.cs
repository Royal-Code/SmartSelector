using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public static class CommentDetails_Extensions
{
    public static IQueryable<CommentDetails> SelectCommentDetails(this IQueryable<Comment> query)
    {
        return query.Select(CommentDetails.SelectCommentExpression);
    }

    public static IEnumerable<CommentDetails> SelectCommentDetails(this IEnumerable<Comment> enumerable)
    {
        return enumerable.Select(CommentDetails.From);
    }

    public static CommentDetails ToCommentDetails(this Comment comment) => CommentDetails.From(comment);
}
