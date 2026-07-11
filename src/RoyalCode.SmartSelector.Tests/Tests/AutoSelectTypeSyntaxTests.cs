using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoSelectTypeSyntaxTests
{
    [Theory]
    [MemberData(nameof(ValidTypeSyntaxes))]
    public void AutoSelect_should_accept_any_valid_class_type_syntax(string source)
    {
        Util.Compile(source, out var output, out var generatorDiagnostics);

        Assert.DoesNotContain(generatorDiagnostics, IsError);
        Assert.True(
            !output.GetDiagnostics().Any(IsSyntaxError),
            string.Join("\n--- GENERATED ---\n", output.SyntaxTrees.Skip(1).Select(tree => tree.ToString())));

        var generatedCode = string.Join("\n", output.SyntaxTrees.Skip(1).Select(tree => tree.ToString()));
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

    private static bool IsError(Diagnostic diagnostic) =>
        diagnostic.Severity == DiagnosticSeverity.Error;

    private static bool IsSyntaxError(Diagnostic diagnostic) =>
        diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id.StartsWith("CS1", StringComparison.Ordinal);

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
