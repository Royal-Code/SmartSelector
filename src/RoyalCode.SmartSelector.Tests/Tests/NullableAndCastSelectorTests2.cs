using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_UserDetails_CanNotCastNullable()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().ContainSingle();

        var error = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        error.Id.Should().Be("RCSS002");
    }
}

file static class Code
{
    public const string Types =
    """
using RoyalCode.SmartSelector;

namespace Tests.SmartSelector.Models;

#nullable disable // poco

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}

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

[AutoSelect<User>]
public partial class UserDetails
{
    public DateTime? LastLogin { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class UserDetails
{
    private static Func<User, UserDetails> selectUserFunc;

    public static Expression<Func<User, UserDetails>> SelectUserExpression { get; } = a => new UserDetails
    {
        LastLogin = a.LastLogin.HasValue ? (DateTime)a.LastLogin.Value : default
    };

    public static UserDetails From(User user) => (selectUserFunc ??= SelectUserExpression.Compile())(user);
}

""";

    public const string ExpectedExtension =
"""

namespace Tests.SmartSelector.Models;

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

""";
}