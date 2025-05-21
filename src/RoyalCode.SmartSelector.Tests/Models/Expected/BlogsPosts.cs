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

internal partial class AuthorDetails
{
    public string Name { get; set; }
}

internal partial class PostDetails
{
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
    public ICollection<CommentDetails> Comments { get; set; } = [];
}

internal partial class CommentDetails
{
    public string Content { get; set; }
    public string AuthorName { get; set; }
}