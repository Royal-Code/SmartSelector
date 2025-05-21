using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

#nullable disable // poco

[AutoSelect<User>]
public partial class UserDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public UserStatusDetails Status { get; set; }
    public DateTimeOffset LastLogin { get; set; }
}

public enum UserStatusDetails
{
    Active,
    Inactive,
    Blocked,
    Suspended,
}