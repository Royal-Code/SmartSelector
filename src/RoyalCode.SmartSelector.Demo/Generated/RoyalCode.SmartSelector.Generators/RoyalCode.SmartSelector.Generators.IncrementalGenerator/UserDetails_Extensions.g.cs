using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Details;

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
