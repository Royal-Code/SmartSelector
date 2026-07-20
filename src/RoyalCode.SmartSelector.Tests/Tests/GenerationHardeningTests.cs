using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class GenerationHardeningTests
{
    [Fact]
    public void Homonymous_dtos_in_different_namespaces_should_have_unique_hint_names()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace First
            {
                public class Entity { public int Id { get; set; } }

                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }

            namespace Second
            {
                public class Entity { public int Id { get; set; } }

                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }
            """);

        result.GeneratedSources.Should().ContainKeys(
            result.GeneratedHintName("First.Details.AutoSelect.g.cs"),
            result.GeneratedHintName("First.Details.Extensions.g.cs"),
            result.GeneratedHintName("Second.Details.AutoSelect.g.cs"),
            result.GeneratedHintName("Second.Details.Extensions.g.cs"));

        result.GeneratedSources.Keys.Should().OnlyContain(static hintName =>
            System.Text.RegularExpressions.Regex.IsMatch(
                hintName,
                "^Details_(AutoSelect|Extensions)\\.[A-Z2-7]{8}\\.g\\.cs$"));
    }

    [Fact]
    public void AutoProperties_should_not_duplicate_a_declared_get_only_property()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace GetOnlyProperty;

            public class Entity
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            [AutoProperties<Entity>]
            public partial class Details
            {
                public string Name => "fixed";
            }
            """);

        result.GeneratedSource("GetOnlyProperty.Details.AutoProperties.g.cs")
            .Should().NotContain("string Name");
    }

    [Fact]
    public void Homonymous_nested_dtos_in_different_containing_types_should_have_unique_artifacts()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace Nested;

            public class Entity { public int Id { get; set; } }

            public partial class First
            {
                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }

            public partial class Second
            {
                [AutoSelect<Entity>]
                public partial class Details { public int Id { get; set; } }
            }
            """);

        result.GeneratedSources.Should().ContainKeys(
            result.GeneratedHintName("Nested.First.Details.AutoSelect.g.cs"),
            result.GeneratedHintName("Nested.First.Details.Extensions.g.cs"),
            result.GeneratedHintName("Nested.Second.Details.AutoSelect.g.cs"),
            result.GeneratedHintName("Nested.Second.Details.Extensions.g.cs"));

        result.GeneratedSource("Nested.First.Details.Extensions.g.cs")
            .Should().Contain("public static class First_Details_Extensions");
        result.GeneratedSource("Nested.Second.Details.Extensions.g.cs")
            .Should().Contain("public static class Second_Details_Extensions");
    }

    [Fact]
    public void Hint_names_should_be_bounded_and_deterministic_for_global_types()
    {
        const string source =
            """
            using RoyalCode.SmartSelector;

            public partial class DetailsWithAnExtremelyLongAndReadableTypeName { }

            namespace HintNames
            {
                public class Address { public int Number { get; set; } }
                public class Entity { public Address Address { get; set; } = new(); }

                [AutoSelect<Entity>, AutoProperties]
                public partial class Details
                {
                    [AutoDetails]
                    public global::DetailsWithAnExtremelyLongAndReadableTypeName Address { get; set; } = new();
                }
            }
            """;

        var first = Util.CompileAndAssert(source);
        var second = Util.CompileAndAssert(source);

        first.GeneratedSources.Keys.Should().BeEquivalentTo(second.GeneratedSources.Keys);
        first.GeneratedSources.Keys.Should().OnlyContain(static hintName => hintName.Length <= 46);
        first.GeneratedSources.Keys.Should().OnlyContain(static hintName =>
            System.Text.RegularExpressions.Regex.IsMatch(
                hintName,
                "^[A-Za-z0-9_-]{1,32}\\.[A-Z2-7]{8}\\.g\\.cs$"));
        first.GeneratedSources.Keys.Should().Contain(static hintName =>
            hintName.Contains("_AutoDetails.", StringComparison.Ordinal));
    }

    [Fact]
    public void Generated_files_should_compile_without_implicit_usings()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace NoImplicitUsings;

            public class Address
            {
                public string City { get; set; } = string.Empty;
            }

            public class Customer
            {
                public int Id { get; set; }
                public Address Address { get; set; } = new();
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDetails Address { get; set; } = new();
            }
            """,
            includeImplicitUsings: false);

        result.GeneratedSources.Should().NotBeEmpty();
        foreach (var source in result.GeneratedSources.Values)
        {
            source.Should().Contain("using System;");
            source.Should().Contain("using System.Linq;");
            source.Should().Contain("using System.Collections.Generic;");
        }
    }

    [Fact]
    public void Generated_members_should_hide_accessible_base_members_with_new()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace Inheritance;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class BaseDetails
            {
                public int Id { get; set; }
            }

            [AutoSelect<Entity>]
            public partial class DerivedDetails : BaseDetails { }
            """);

        var generated = result.GeneratedSource("Inheritance.DerivedDetails.AutoSelect.g.cs");
        generated.Should().Contain("public static new Expression<Func<Entity, DerivedDetails>> SelectEntityExpression");
        generated.Should().Contain("public static new DerivedDetails From(Entity entity)");
        generated.Should().NotContain("private static new Func");
        result.CompilationDiagnostics.Should().NotContain(
            diagnostic => diagnostic.Id == "CS0108" || diagnostic.Id == "CS0109");
    }

    [Fact]
    public void Generated_members_without_base_conflicts_should_not_use_new()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            namespace NoInheritance;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class Details { public int Id { get; set; } }
            """);

        result.GeneratedSource("NoInheritance.Details.AutoSelect.g.cs")
            .Should().NotContain(" static new ");
    }
}
