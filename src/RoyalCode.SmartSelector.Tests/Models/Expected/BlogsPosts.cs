using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

#nullable disable

internal class Blog : Entity<string>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public Author Author { get; set; }
    public ICollection<Post> Posts { get; set; } = [];
}

internal class Author : Entity<Guid>
{
    public string Name { get; set; }
    public string Bio { get; set; }
    public User User { get; set; }
}

internal class Post : Entity<Guid>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public Author Author { get; set; }
    public Blog Blog { get; set; }
    public ICollection<Comment> Comments { get; set; } = [];
}

internal class Comment : Entity<Guid>
{
    public string Content { get; set; }
    public Post Post { get; set; }
    public User Author { get; set; }
}

// dtos

internal partial class BlogDetails
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
    public ICollection<PostDetails> Posts { get; set; } = [];
}

[AutoSelect<Author>]
internal partial class AuthorDetails
{
    public string Name { get; set; }
}

[AutoSelect<Post>]
internal partial class PostDetails
{
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
}

[AutoSelect<Post>]
internal partial class PostAndCommentsDetails : PostDetails
{
    public IReadOnlyList<CommentDetails> Comments { get; set; } = [];
}

internal partial class CommentDetails
{
    public string Content { get; set; }
    public string AuthorName { get; set; }
}

// expect

internal partial class PostDetails
{
    private static Func<Post, PostDetails> selectPostFunc;

    public static Expression<Func<Post, PostDetails>> SelectPostExpression { get; } = a => new PostDetails
    {
        Title = a.Title,
        Content = a.Content,
        Author = new AuthorDetails
        {
            Name = a.Author.Name,
        },
    };

    public static PostDetails From(Post post) => (selectPostFunc ??= SelectPostExpression.Compile())(post);
}

internal partial class PostAndCommentsDetails
{
    private static Func<Post, PostAndCommentsDetails> selectPostFunc;

    public static new Expression<Func<Post, PostAndCommentsDetails>> SelectPostExpression { get; } = a => new PostAndCommentsDetails
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

    public static new PostAndCommentsDetails From(Post post) => (selectPostFunc ??= SelectPostExpression.Compile())(post);
}