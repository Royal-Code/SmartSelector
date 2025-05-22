namespace RoyalCode.SmartSelector.Demo.Entities.Blogs;

#nullable disable

public class Blog : Entity<string>
{
    public Blog()
    {
        Id = Guid.NewGuid().ToString();
    }

    public string Title { get; set; }
    public string Content { get; set; }
    public Author Author { get; set; }
    public ICollection<Post> Posts { get; set; } = [];
}
