using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

#region Domain / Entities

public class User : Entity<Guid>
{
    public User(string name)
    {
        Name = name;
        Status = Status.Active;
    }
    public string Name { get; set; }

    public Status Status { get; set; }

    public DateTimeOffset? LastLogin { get; set; }
}

public enum Status
{
    Active,
    Inactive,
    Blocked,
    Suspended,
}

#endregion

#region Application / DTOs

[AutoSelect<User>]
public partial class UserDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public StatusDetails Status { get; set; }
    public DateTimeOffset LastLogin { get; set; }
}

public enum StatusDetails
{
    Active,
    Inactive,
    Blocked,
    Suspended,
}

#endregion

#region Expected generated code

public partial class UserDetails
{
    private static Func<User, UserDetails> selectUserFunc;

    public static Expression<Func<User, UserDetails>> SelectUserExpression { get; } = a => new UserDetails
    {
        Id = a.Id,
        Name = a.Name,
        Status = (StatusDetails)a.Status,
        LastLogin = a.LastLogin.HasValue ? a.LastLogin.Value : default
    };

    public static UserDetails From(User user) => (selectUserFunc ??= SelectUserExpression.Compile())(user);
}

public static class UserDetails_Extensions
{
    public static IQueryable<UserDetails> SelectUserDetails(this IQueryable<User> query)
    {
        return query.Select(UserDetails.SelectUserExpression);
    }

    public static IEnumerable<UserDetails> SelectUserDetails(this IEnumerable<User> enumerable)
    {
        return enumerable.Select(UserDetails.From);
    }

    public static UserDetails ToUserDetails(this User user) => UserDetails.From(user);
}

#endregion