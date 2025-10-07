using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class NewInstanceTests
{
    [Fact]
    public void Select_PostDetails()
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

[AutoSelect<Post>]
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

""";
    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

internal partial class PostDetails
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

""";
}

