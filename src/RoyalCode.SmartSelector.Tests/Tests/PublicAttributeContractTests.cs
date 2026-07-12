using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class PublicAttributeContractTests
{
    [Fact]
    public void Public_attributes_should_reject_consumer_inheritance()
    {
        var result = Util.Compile(
            """
            using RoyalCode.SmartSelector;

            namespace Consumer;

            public class Entity { }

            public class CustomAutoSelect : AutoSelectAttribute<Entity> { }
            public class CustomAutoProperties : AutoPropertiesAttribute { }
            public class CustomTypedAutoProperties : AutoPropertiesAttribute<Entity> { }
            public class CustomAutoDetails : AutoDetailsAttribute { }
            public class CustomOptions : AutoPropertiesAttributeBase { }
            """);

        var inheritanceErrors = result.CompilationDiagnostics
            .Where(diagnostic => diagnostic.Id == "CS0509")
            .ToArray();

        inheritanceErrors.Should().HaveCount(4);
        var messages = inheritanceErrors.Select(diagnostic => diagnostic.GetMessage()).ToArray();
        messages.Should().Contain(message =>
            message.Contains("AutoSelectAttribute", StringComparison.Ordinal));
        messages.Should().Contain(message =>
            message.Contains("AutoPropertiesAttribute", StringComparison.Ordinal));
        messages.Should().Contain(message =>
            message.Contains("AutoDetailsAttribute", StringComparison.Ordinal));
    }
}
