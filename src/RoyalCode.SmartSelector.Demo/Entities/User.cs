namespace RoyalCode.SmartSelector.Demo.Entities;

public class User : Entity<Guid>
{
    public User(string name)
    {
        Name = name;
        Status = UserStatus.Active;
    }

    public string Name { get; set; }

    public UserStatus Status { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public UserSpecialization? Specialization { get; set; }
}

public enum UserStatus
{
    Active,
    Inactive,
    Blocked,
    Suspended,
}

public enum UserSpecialization
{
    None,
    Developer,
    Designer,
    Manager,
    Tester,
}