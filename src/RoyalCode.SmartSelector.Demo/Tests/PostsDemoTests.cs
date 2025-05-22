using RoyalCode.SmartSelector.Demo.Details.Blogs;
using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using RoyalCode.SmartSelector.Demo.Infra;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace RoyalCode.SmartSelector.Demo.Tests;

public class PostsDemoTests
{
    [Fact]
    public void GetSelectExpression()
    {
        // act
        var expression = PostDetails.SelectPostExpression;
        // assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void Create_PostDetails_From_Post()
    {
        // arrange
        var author = new Author { Name = "Author 1" };
        var blog = new Blog { Title = "Blog 1", Content = "Blog Content", Author = author };
        var post = new Post { Title = "Post 1", Content = "Content 1", Author = author, Blog = blog };

        // act
        var details = PostDetails.From(post);

        // assert
        Assert.NotNull(details);
        Assert.Equal(post.Title, details.Title);
        Assert.Equal(post.Content, details.Content);
        Assert.NotNull(details.Author);
        Assert.Equal(author.Name, details.Author.Name);
    }

    [Fact]
    public void Create_PostDetails_Using_ToPostDetails()
    {
        // arrange
        var author = new Author { Name = "Author 1" };
        var blog = new Blog { Title = "Blog 1", Content = "Blog Content", Author = author };
        var post = new Post { Title = "Post 1", Content = "Content 1", Author = author, Blog = blog };

        // act
        var details = post.ToPostDetails();

        // assert
        Assert.NotNull(details);
        Assert.Equal(post.Title, details.Title);
        Assert.Equal(post.Content, details.Content);
        Assert.NotNull(details.Author);
        Assert.Equal(author.Name, details.Author.Name);
    }

    [Fact]
    public void Query_PostDetails_From_Database()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        var author = new Author { Name = "Author 1" };
        var blog = new Blog { Title = "Blog 1", Content = "Blog Content", Author = author };
        db.Authors.Add(author);
        db.Blogs.Add(blog);
        db.SaveChanges();
        db.Posts.Add(new Post { Title = "Post 1", Content = "Content 1", Author = author, Blog = blog });
        db.Posts.Add(new Post { Title = "Post 2", Content = "Content 2", Author = author, Blog = blog });
        db.Posts.Add(new Post { Title = "Post 3", Content = "Content 3", Author = author, Blog = blog });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act
        var details = db.Posts
            .SelectPostDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Title == "Post 1");
        Assert.Contains(details, d => d.Title == "Post 2");
        Assert.Contains(details, d => d.Title == "Post 3");
    }

    [Fact]
    public void Select_PostDetails_From_List()
    {
        // arrange
        var author = new Author { Name = "Author 1" };
        var blog = new Blog { Title = "Blog 1", Content = "Blog Content", Author = author };
        var posts = new List<Post>
        {
            new Post { Title = "Post 1", Content = "Content 1", Author = author, Blog = blog },
            new Post { Title = "Post 2", Content = "Content 2", Author = author, Blog = blog },
            new Post { Title = "Post 3", Content = "Content 3", Author = author, Blog = blog }
        };

        // act
        var details = posts
            .SelectPostDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Title == "Post 1");
        Assert.Contains(details, d => d.Title == "Post 2");
        Assert.Contains(details, d => d.Title == "Post 3");
    }
}
