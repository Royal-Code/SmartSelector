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
        }
    };

    public static PostDetails From(Post post) => (selectPostFunc ??= SelectPostExpression.Compile())(post);
}
