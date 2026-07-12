using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoPropertiesSemanticTests
{
    [Fact]
    public void Qualified_and_aliased_attributes_should_match_the_simple_form()
    {
        var simple = CompileAutoProperties("AutoProperties<Entity>",
            "using RoyalCode.SmartSelector;");
        var qualified = CompileAutoProperties(
            "global::RoyalCode.SmartSelector.AutoProperties<Semantic.Entity>",
            string.Empty);
        var aliased = CompileAutoProperties(
            "AutoProps",
            "using AutoProps = RoyalCode.SmartSelector.AutoPropertiesAttribute<Semantic.Entity>;");

        qualified.Should().Be(simple);
        aliased.Should().Be(simple);
        simple.Should().Contain("public int Id { get; set; }");
        simple.Should().Contain("public string NestedCode { get; set; }");
        simple.Should().NotContain("public string Name { get; set; }");
        simple.Should().NotContain("public Nested Nested { get; set; }");
    }

    [Fact]
    public void Aliased_MapFrom_should_resolve_its_constructor_argument_semantically()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;
            using SourceProperty = RoyalCode.SmartSelector.MapFromAttribute;

            namespace Semantic;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class Details
            {
                [SourceProperty(nameof(Entity.Id))]
                public int CustomId { get; set; }
            }
            """);

        result.GeneratedSource("Semantic.Details.AutoSelect.g.cs")
            .Should().Contain("CustomId = a.Id");
    }

    private static string CompileAutoProperties(string attributeName, string usingDirective)
    {
        var source = $$"""
            {{usingDirective}}

            namespace Semantic;

            public class Nested
            {
                public string Code { get; set; } = string.Empty;
            }

            public class Entity
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public Nested Nested { get; set; } = new();
            }

            [{{attributeName}}(
                Exclude = new[] { nameof(Entity.Name) },
                Flattening = new[] { nameof(Entity.Nested) })]
            public partial class Details { }
            """;

        var result = Util.CompileAndAssert(source);
        return result.GeneratedSource("Semantic.Details.AutoProperties.g.cs");
    }
}
