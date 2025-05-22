using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public partial class PostDetails
{
    private static Func<Post, PostDetails> selectPostFunc;

    public static Expression<Func<Post, PostDetails>> SelectPostExpression { get; } = a => new PostDetails
    {
        Title = a.Title,
        Content = a.Content,
        Author = new AuthorDetails
        {
            Name = a.Author.Name
        },
        Comments = a.Comments.Select(b => new CommentDetails
        {
            Content = b.Content,
            AuthorName = b.Author.Name
        }).ToList()
    };

    public static PostDetails From(Post post) => (selectPostFunc ??= SelectPostExpression.Compile())(post);
}
