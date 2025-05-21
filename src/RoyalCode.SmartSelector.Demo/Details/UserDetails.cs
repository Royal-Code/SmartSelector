using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

#nullable disable // poco

[AutoSelect<User>]
public partial class UserDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public StatusDetails Status { get; set; }
    public DateTimeOffset LastLogin { get; set; }
    public UserSpecializationDetails Specialization { get; set; }
}

public enum StatusDetails
{
    Active,
    Inactive,
    Blocked,
    Suspended,
}

public enum UserSpecializationDetails
{
    None,
    Developer,
    Designer,
    Manager,
    Tester,
}