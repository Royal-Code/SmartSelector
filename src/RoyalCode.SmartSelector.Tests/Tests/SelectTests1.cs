using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class SelectTests
{
    [Fact]
    public void Select_PostAndCommentsDetails()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);
    }
}

file static class Code
{
    public const string Types =
"""
using RoyalCode.SmartSelector;
using System.Collections.Generic;

namespace Tests.SmartSelector.Models;

#nullable disable // poco

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}

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

public class User : Entity<Guid>
{
    public User(string name)
    {
        Name = name;
        Status = Status.Active;
    }

    public string Name { get; set; }

    public Status Status { get; set; }

    public DateTimeOffset? LastLogin { get; set; }
}

public enum Status
{
    Active,
    Inactive,
    Blocked,
    Suspended,
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

internal partial class PostDetails
{
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
}

internal partial class AuthorDetails
{
    public string Name { get; set; }
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

""";
    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

internal partial class PostAndCommentsDetails
{
    private static Func<Post, PostAndCommentsDetails> selectPostFunc;

    public static Expression<Func<Post, PostAndCommentsDetails>> SelectPostExpression { get; } = a => new PostAndCommentsDetails
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

    public static PostAndCommentsDetails From(Post post) => (selectPostFunc ??= SelectPostExpression.Compile())(post);
}

""";
}

