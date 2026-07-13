namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Compilation tests for generator scenarios that previously represented known limitations.
/// </summary>
public class GeneratedCodeCompilationTddTests
{
    [Fact]
    public void Generated_code_should_compile_for_a_nested_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            namespace NestedAutoSelect;

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
    public void Generated_AutoProperties_should_compile_for_a_nested_destination_dto()
    {
        AssertGeneratedCodeCompiles(
            """
            using RoyalCode.SmartSelector;

            namespace NestedAutoProperties;

            public class Entity { public int Id { get; set; } }

            public partial class Container
            {
                [AutoProperties<Entity>]
                internal partial class EntityDetails
                {
                }

                internal static int Read(EntityDetails value) => value.Id;
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
