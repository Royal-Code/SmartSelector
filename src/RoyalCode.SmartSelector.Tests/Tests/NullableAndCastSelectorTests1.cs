using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void SimpleCast_NullableTernary_Select_UserDetails()
    {
        var result = Util.CompileAndAssert(Code.Types);

        var generatedInterface = result.GeneratedSource("Tests.SmartSelector.Models.UserDetails.AutoSelect.g.cs");
        generatedInterface.Should().Be(Code.ExpectedPartial);

        var generatedHandler = result.GeneratedSource("Tests.SmartSelector.Models.UserDetails.Extensions.g.cs");
        generatedHandler.Should().Be(Code.ExpectedExtension);
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
""";

    public const string ExpectedPartial =
"""
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

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

""";

    public const string ExpectedExtension =
"""
using System;
using System.Linq;
using System.Collections.Generic;

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
