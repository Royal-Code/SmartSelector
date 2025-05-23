using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public partial class NullableAndCastSelectorTests
{
    [Fact]
    public void Select_UserDetails_CastAndNullable()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);

        var generatedHandler = output.SyntaxTrees.Skip(2).FirstOrDefault()?.ToString();
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

    public UserSpecialization? Specialization { get; set; }
}

public enum Status
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
        Id = a.Id,
        Name = a.Name,
        Status = (StatusDetails)a.Status,
        LastLogin = a.LastLogin.HasValue ? a.LastLogin.Value : default,
        Specialization = a.Specialization.HasValue ? (UserSpecializationDetails)a.Specialization.Value : default
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