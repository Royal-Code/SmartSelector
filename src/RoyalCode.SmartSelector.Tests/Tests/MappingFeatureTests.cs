using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class MappingFeatureTests
{
    [Fact]
    public void AutoSelect_configuration_should_generate_excluded_and_flattened_properties_and_execute()
    {
        var result = Util.CompileFast(
            """
            using RoyalCode.SmartSelector;

            namespace MappingFeatures;

            public class Address { public string City { get; set; } = string.Empty; }
            public class Entity
            {
                public int Id { get; set; }
                public string Secret { get; set; } = string.Empty;
                public Address Address { get; set; } = new();
            }

            [AutoSelect<Entity>(
                Exclude = [nameof(Entity.Secret)],
                Flattening = [nameof(Entity.Address)])]
            public partial class Details { }

            public static class Scenario
            {
                public static string Run()
                {
                    var value = Details.From(new Entity
                    {
                        Id = 42,
                        Secret = "hidden",
                        Address = new Address { City = "Porto" },
                    });
                    return $"{value.Id}:{value.AddressCity}";
                }
            }
            """);

        result.Errors.Should().BeEmpty();
        result.GeneratedSource("MappingFeatures.Details.AutoProperties.g.cs")
            .Should().Contain("public int Id").And.Contain("public string AddressCity").And.NotContain("Secret");
        Execute(result).Should().Be("42:Porto");
    }

    [Fact]
    public void Arrays_should_be_generated_and_complex_items_should_project_with_ToArray()
    {
        var result = Util.CompileFast(
            """
            using RoyalCode.SmartSelector;

            namespace MappingFeatures;

            public class Item { public string Name { get; set; } = string.Empty; }
            public class Entity
            {
                public int[] Scores { get; set; } = [];
                public Item[]? Items { get; set; }
            }

            [AutoSelect<Entity>, AutoProperties]
            public partial class Details
            {
                public ItemDetails[] Items { get; set; } = [];
            }

            public class ItemDetails { public string Name { get; set; } = string.Empty; }

            public static class Scenario
            {
                public static string Run()
                {
                    var value = Details.From(new Entity
                    {
                        Scores = [3, 5],
                        Items = [new Item { Name = "A" }, new Item { Name = "B" }],
                    });
                    var empty = Details.From(new Entity { Scores = [], Items = null });
                    return $"{string.Join(',', value.Scores)}:{string.Join(',', value.Items.Select(x => x.Name))}:{empty.Items.Length}";
                }
            }
            """);

        result.Errors.Should().BeEmpty();
        result.GeneratedSource("MappingFeatures.Details.AutoProperties.g.cs")
            .Should().Contain("public int[] Scores");
        result.GeneratedSource("MappingFeatures.Details.AutoSelect.g.cs")
            .Should().Contain("Items = a.Items == null ? Array.Empty<ItemDetails>() : a.Items.Select(")
            .And.Contain("}).ToArray()");
        Execute(result).Should().Be("3,5:A,B:0");
    }

    [Fact]
    public void Nested_MapFrom_should_use_the_explicit_path_and_execute()
    {
        var result = Util.CompileFast(
            """
            using RoyalCode.SmartSelector;

            namespace MappingFeatures;

            public class Address { public string City { get; set; } = string.Empty; }
            public class Entity
            {
                public string AddressCity { get; set; } = "wrong";
                public Address Address { get; set; } = new();
            }

            [AutoSelect<Entity>]
            public partial class Details
            {
                [MapFrom("Address.City")]
                public string City { get; set; } = string.Empty;
            }

            public static class Scenario
            {
                public static string Run() => Details.From(new Entity
                {
                    AddressCity = "wrong",
                    Address = new Address { City = "right" },
                }).City;
            }
            """);

        result.Errors.Should().BeEmpty();
        result.GeneratedSource("MappingFeatures.Details.AutoSelect.g.cs")
            .Should().Contain("City = a.Address.City").And.NotContain("City = a.AddressCity");
        Execute(result).Should().Be("right");
    }

    [Fact]
    public void Invalid_nested_MapFrom_should_report_RCSS017_on_the_destination_property()
    {
        const string source =
            """
            using RoyalCode.SmartSelector;

            namespace MappingFeatures;

            public class Entity { public string Name { get; set; } = string.Empty; }

            [AutoSelect<Entity>]
            public partial class Details
            {
                [MapFrom("Address.Missing")]
                public string City { get; set; } = string.Empty;
            }
            """;
        var result = Util.Compile(source);
        var diagnostic = result.GeneratorDiagnostics.Single(item => item.Id == "RCSS017");
        source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length)
            .Should().Be("City");
    }

    private static object? Execute(CompileResult result)
    {
        using var stream = new MemoryStream();
        var emit = result.OutputCompilation.Emit(stream);
        emit.Success.Should().BeTrue(string.Join(Environment.NewLine, emit.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        return assembly.GetType("MappingFeatures.Scenario")!
            .GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, null);
    }
}
