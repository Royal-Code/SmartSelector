using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoSelectTypeSyntaxTests
{
    [Theory]
    [MemberData(nameof(ValidTypeSyntaxes))]
    public void AutoSelect_should_accept_any_valid_class_type_syntax(string source)
    {
        var result = Util.CompileAndAssert(source);
        var generatedCode = result.AllGeneratedSources();
        if (source == NestedType)
            Assert.Contains("Users.User", generatedCode, StringComparison.Ordinal);

        if (source == ConstructedGenericType)
            Assert.Contains("Envelope<string>", generatedCode, StringComparison.Ordinal);
    }

    public static TheoryData<string> ValidTypeSyntaxes => new()
    {
        QualifiedType,
        GlobalAliasType,
        NestedType,
        ConstructedGenericType,
    };

    private const string QualifiedType =
        """
        using RoyalCode.SmartSelector;

        namespace Domain
        {
            public class User { public string Name { get; set; } = ""; }
        }

        namespace Dtos
        {
            [AutoSelect<Domain.User>]
            public partial class UserDetails { public string Name { get; set; } = ""; }
        }
        """;

    private const string GlobalAliasType =
        """
        using RoyalCode.SmartSelector;

        namespace Domain
        {
            public class User { public string Name { get; set; } = ""; }
        }

        namespace Dtos
        {
            [AutoSelect<global::Domain.User>]
            public partial class UserDetails { public string Name { get; set; } = ""; }
        }
        """;

    private const string NestedType =
        """
        using RoyalCode.SmartSelector;

        namespace Domain
        {
            public static class Users
            {
                public class User { public string Name { get; set; } = ""; }
            }
        }

        namespace Dtos
        {
            [AutoSelect<Domain.Users.User>]
            public partial class UserDetails { public string Name { get; set; } = ""; }
        }
        """;

    private const string ConstructedGenericType =
        """
        using RoyalCode.SmartSelector;

        namespace Domain
        {
            public class Envelope<T> { public T Value { get; set; } = default!; }
        }

        namespace Dtos
        {
            [AutoSelect<Domain.Envelope<string>>]
            public partial class StringEnvelopeDetails { public string Value { get; set; } = ""; }
        }
        """;
}
