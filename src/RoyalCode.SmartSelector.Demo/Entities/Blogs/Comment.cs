namespace RoyalCode.SmartSelector.Demo.Entities.Blogs;

#nullable disable

public class Comment : Entity<Guid>
{
    public Comment()
    {
        Id = Guid.NewGuid();
    }

    public string Content { get; set; }
    public Post Post { get; set; }
    public User Author { get; set; }
}
