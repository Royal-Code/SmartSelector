#nullable disable

using RoyalCode.SmartSelector.Demo.Entities.Blogs;

namespace RoyalCode.SmartSelector.Demo.Details.Blogs;

[AutoSelect<Author>]
public partial class AuthorDetails
{
    public string Name { get; set; }
}
