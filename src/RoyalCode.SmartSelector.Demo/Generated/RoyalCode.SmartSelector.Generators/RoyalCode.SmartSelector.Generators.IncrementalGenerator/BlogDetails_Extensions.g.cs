using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public static class BlogDetails_Extensions
{
    public static IQueryable<BlogDetails> SelectBlogDetails(this IQueryable<Blog> query)
    {
        return query.Select(BlogDetails.SelectBlogExpression);
    }

    public static IEnumerable<BlogDetails> SelectBlogDetails(this IEnumerable<Blog> enumerable)
    {
        return enumerable.Select(BlogDetails.From);
    }

    public static BlogDetails ToBlogDetails(this Blog blog) => BlogDetails.From(blog);
}
