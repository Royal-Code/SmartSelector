#nullable disable

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

public partial class BlogDetails
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
    public ICollection<PostDetails> Posts { get; set; } = [];
}
