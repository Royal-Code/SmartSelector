namespace RoyalCode.SmartSelector.Demo.Entities.Blogs;

#nullable disable

public class Author : Entity<Guid>
{
    public Author()
    {
        Id = Guid.NewGuid();
    }

    public string Name { get; set; }
    public string Bio { get; set; }
    public User User { get; set; }
}
