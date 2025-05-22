#nullable disable

using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

[AutoSelect<Comment>]
public partial class CommentDetails
{
    public string Content { get; set; }
    public string AuthorName { get; set; }
}