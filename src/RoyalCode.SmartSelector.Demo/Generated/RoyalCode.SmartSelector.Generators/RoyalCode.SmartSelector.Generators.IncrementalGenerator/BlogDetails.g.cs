using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public partial class BlogDetails
{
    private static Func<Blog, BlogDetails> selectBlogFunc;

    public static Expression<Func<Blog, BlogDetails>> SelectBlogExpression { get; } = a => new BlogDetails
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Author = new AuthorDetails
        {
            Name = a.Author.Name
        },
        Posts = a.Posts.Select(b => new PostDetails
        {
            Title = b.Title,
            Content = b.Content,
            Author = new AuthorDetails
            {
                Name = b.Author.Name
            },
            Comments = b.Comments.Select(c => new CommentDetails
            {
                Content = c.Content,
                AuthorName = c.Author.Name
            }).ToList()
        }).ToList()
    };

    public static BlogDetails From(Blog blog) => (selectBlogFunc ??= SelectBlogExpression.Compile())(blog);
}
