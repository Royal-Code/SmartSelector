namespace RoyalCode.SmartSelector.Demo.Entities.Blogs;

#nullable disable

public class Post : Entity<Guid>
{
    public Post()
    {
        Id = Guid.NewGuid();
    }

    public string Title { get; set; }
    public string Content { get; set; }
    public Author Author { get; set; }
    public Blog Blog { get; set; }
    public ICollection<Comment> Comments { get; set; } = [];
}
