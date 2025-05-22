#nullable disable

using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

[AutoSelect<Post>]
public partial class PostDetails
{
    public string Title { get; set; }
    public string Content { get; set; }
    public AuthorDetails Author { get; set; }
    //public ICollection<CommentDetails> Comments { get; set; } = [];
}
