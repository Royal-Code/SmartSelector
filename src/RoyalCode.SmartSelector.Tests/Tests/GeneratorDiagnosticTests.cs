using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class GeneratorDiagnosticTests
{
    [Fact]
    public void RCSS006_should_report_non_partial_AutoProperties_class()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity { public int Id { get; set; } }

            [AutoProperties<Entity>]
            public class Details { }
            """,
            "RCSS006",
            "Details");
    }

    [Fact]
    public void RCSS007_should_report_orphan_non_generic_AutoProperties()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            [AutoProperties]
            public partial class Details { }
            """,
            "RCSS007",
            "AutoProperties");
    }

    [Fact]
    public void RCSS008_should_report_generic_destination_dto()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class Details<T> { public int Id { get; set; } }
            """,
            "RCSS008",
            "Details");
    }

    [Fact]
    public void RCSS008_should_report_generic_containing_type()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity { public int Id { get; set; } }

            public partial class Container<T>
            {
                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }
            """,
            "RCSS008",
            "Details");
    }

    [Fact]
    public void RCSS000_should_report_non_partial_containing_type()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity { public int Id { get; set; } }

            public class Container
            {
                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }
            """,
            "RCSS000",
            "Details");
    }

    [Fact]
    public void RCSS010_should_warn_about_ambiguous_flattening()
    {
        var result = AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Address { public string City { get; set; } = string.Empty; }
            public class Customer { public Address Address { get; set; } = new(); }
            public class CustomerAddress { public string City { get; set; } = string.Empty; }
            public class Entity
            {
                public Customer Customer { get; set; } = new();
                public CustomerAddress CustomerAddress { get; set; } = new();
            }

            [AutoSelect<Entity>]
            public partial class Details
            {
                public string CustomerAddressCity { get; set; } = string.Empty;
            }
            """,
            "RCSS010",
            "CustomerAddressCity");

        result.GeneratorDiagnostics.Single(diagnostic => diagnostic.Id == "RCSS010")
            .Severity.Should().Be(DiagnosticSeverity.Warning);
        result.Errors.Should().BeEmpty();
        result.GeneratedSources.Should().ContainKey("Diagnostics.Details.AutoSelect.g.cs");
    }

    [Fact]
    public void RCSS011_should_report_destination_dto_in_global_namespace()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class Details { public int Id { get; set; } }
            """,
            "RCSS011",
            "Details");
    }

    [Fact]
    public void RCSS001_from_AutoDetails_should_point_to_the_property()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity { public int Id { get; set; } }

            [AutoProperties<Entity>]
            public partial class Details
            {
                [AutoDetails]
                public AddressDetails Address { get; set; } = new();
            }

            public partial class AddressDetails { }
            """,
            "RCSS001",
            "Address");
    }

    private static CompileResult AssertDiagnostic(string source, string id, string expectedLocationText)
    {
        var result = Util.Compile(source);
        var diagnostic = result.GeneratorDiagnostics.Single(item => item.Id == id);

        diagnostic.Location.Should().NotBe(Location.None);
        diagnostic.Location.IsInSource.Should().BeTrue();
        diagnostic.Location.SourceSpan.Length.Should().BeGreaterThan(0);

        var actualLocationText = source.Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
        actualLocationText.Should().Be(expectedLocationText);

        return result;
    }
}
