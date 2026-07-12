namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Characterization tests for known generator limitations.
/// These tests intentionally remain red until each corresponding production fix is implemented.
/// </summary>
public class GeneratedCodeCompilationTddTests
{
    [Fact]
    [Trait("Category", "KnownLimitation")]
    // Fase 13: DTOs genéricos serão rejeitados por diagnóstico permanente.
    public void Generated_code_should_compile_for_a_generic_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            public class Entity { public int Id { get; set; } }

            [AutoSelect<Entity>]
            public partial class EntityDetails<T>
            {
                public int Id { get; set; }
            }
            """);
    }

    [Fact]
    [Trait("Category", "KnownLimitation")]
    // Fase 13: suporte a DTOs aninhados por cadeia de containing types.
    public void Generated_code_should_compile_for_a_nested_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            public class Entity { public int Id { get; set; } }

            public partial class Container
            {
                [AutoSelect<Entity>]
                public partial class EntityDetails
                {
                    public int Id { get; set; }
                }

                public static EntityDetails Map(Entity value) => EntityDetails.From(value);
            }
            """);
    }

    [Fact]
    public void Generated_code_should_compile_with_a_fully_qualified_AutoProperties_attribute()
    {
        AssertGeneratedCodeCompiles(
            """
            namespace Domain
            {
                public class Entity { public int Id { get; set; } }
            }

            namespace Dtos
            {
                [global::RoyalCode.SmartSelector.AutoProperties<Domain.Entity>]
                public partial class EntityDetails { }

                public static class Consumer
                {
                    public static int Read(EntityDetails value) => value.Id;
                }
            }
            """);
    }

    private static void AssertGeneratedCodeCompiles(string source)
    {
        foreach (var targetFramework in Enum.GetValues<TestTargetFramework>())
        {
            var result = Util.Compile(source, targetFramework);
            Assert.True(
                !result.Errors.Any(),
                $"Generated code failed for {targetFramework}:{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");
        }
    }
}
