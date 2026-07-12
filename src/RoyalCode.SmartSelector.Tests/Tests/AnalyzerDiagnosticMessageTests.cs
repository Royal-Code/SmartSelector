using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AnalyzerDiagnosticMessageTests
{
    [Fact]
    public void RCSS003_should_name_the_AutoPropertiesAttribute()
    {
        var result = Util.Compile(
            """
            using RoyalCode.SmartSelector;

            namespace Diagnostics;

            public class Entity
            {
                public int Id { get; set; }
            }

            [AutoSelect<Entity>, AutoProperties<Entity>]
            public partial class EntityDetails { }
            """);

        var diagnostic = result.GeneratorDiagnostics.Should()
            .ContainSingle(item => item.Id == "RCSS003")
            .Which;

        var obsoleteAttributeName = "AutoProperty" + "Attribute";
        diagnostic.GetMessage().Should()
            .Contain("AutoPropertiesAttribute<TFrom>")
            .And.Contain("AutoPropertiesAttribute")
            .And.NotContain(obsoleteAttributeName);
    }
}
