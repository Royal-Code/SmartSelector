using RoyalCode.SmartSelector.Demo.Entities;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Demo.Details;

public partial class UserDetails
{
    private static Func<User, UserDetails> selectUserFunc;

    public static Expression<Func<User, UserDetails>> SelectUserExpression { get; } = a => new UserDetails
    {
        Id = a.Id,
        Name = a.Name,
        Status = (UserStatusDetails)a.Status,
        LastLogin = a.LastLogin.HasValue ? a.LastLogin.Value : default
    };

    public static UserDetails From(User user) => (selectUserFunc ??= SelectUserExpression.Compile())(user);
}
