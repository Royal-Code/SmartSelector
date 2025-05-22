using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public static class PostDetails_Extensions
{
    public static IQueryable<PostDetails> SelectPostDetails(this IQueryable<Post> query)
    {
        return query.Select(PostDetails.SelectPostExpression);
    }

    public static IEnumerable<PostDetails> SelectPostDetails(this IEnumerable<Post> enumerable)
    {
        return enumerable.Select(PostDetails.From);
    }

    public static PostDetails ToPostDetails(this Post post) => PostDetails.From(post);
}
