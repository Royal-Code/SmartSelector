using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class DocumentationSnippetCompilationTests
{
    [Fact]
    public void Quickstart_AutoSelect_with_AutoProperties_should_compile()
    {
        var result = CompileDocumentationSnippet(
            """
            using RoyalCode.SmartSelector;

            namespace Documentation.Quickstart;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            [AutoSelect<User>, AutoProperties]
            public partial class UserDetails { }
            """);

        result.GeneratedSources.Should().ContainKeys(
            "UserDetails.AutoProperties.g.cs",
            "UserDetails.g.cs",
            "UserDetails_Extensions.g.cs");
    }

    [Fact]
    public void Isolated_generic_AutoProperties_should_compile()
    {
        var result = CompileDocumentationSnippet(
            """
            using RoyalCode.SmartSelector;

            namespace Documentation.AutoProperties;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            [AutoProperties<User>]
            public partial class UserSnapshot { }
            """);

        result.GeneratedSources.Should().ContainKey("UserSnapshot.AutoProperties.g.cs");
    }

    [Fact]
    public void Nested_dto_with_AutoDetails_should_compile()
    {
        var result = CompileDocumentationSnippet(
            """
            using RoyalCode.SmartSelector;

            namespace Documentation.AutoDetails;

            public class Address
            {
                public string City { get; set; } = string.Empty;
                public string Street { get; set; } = string.Empty;
            }

            public class Customer
            {
                public int Id { get; set; }
                public Address Address { get; set; } = new();
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                public int Id { get; set; }

                [AutoDetails]
                public AddressDetails Address { get; set; } = new();
            }
            """);

        result.GeneratedSources.Should().ContainKeys(
            "AddressDetails.AutoDetails.g.cs",
            "CustomerDetails.g.cs",
            "CustomerDetails_Extensions.g.cs");
    }

    [Fact]
    public void Flattening_by_convention_should_compile()
    {
        var result = CompileDocumentationSnippet(
            """
            using RoyalCode.SmartSelector;

            namespace Documentation.Flattening;

            public class Address
            {
                public string City { get; set; } = string.Empty;
            }

            public class Customer
            {
                public Address Address { get; set; } = new();
            }

            public class Order
            {
                public Customer Customer { get; set; } = new();
            }

            [AutoSelect<Order>]
            public partial class OrderDetails
            {
                public string CustomerAddressCity { get; set; } = string.Empty;
            }
            """);

        result.GeneratedSource("OrderDetails.g.cs")
            .Should().Contain("CustomerAddressCity = a.Customer.Address.City");
    }

    private static CompileResult CompileDocumentationSnippet(string source)
    {
        var result = Util.CompileAndAssert(source);
        result.GeneratorDiagnostics.Should().NotContain(
            diagnostic => diagnostic.Id.StartsWith("RCSS", StringComparison.Ordinal));
        return result;
    }
}
