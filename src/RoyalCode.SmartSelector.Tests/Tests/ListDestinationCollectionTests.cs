using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Regression for a bug found via RoyalCode.SmartCommands.Demo (see that repo's
/// <c>.docs/plans/plan-demo-usage-scenarios.md</c>, "Registro de gaps"): when a DTO declares a nested object
/// collection using the concrete <c>List&lt;T&gt;</c> type (instead of <c>IReadOnlyList&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>),
/// the generator used to emit an invalid object initializer for the destination <c>List&lt;T&gt;</c> itself,
/// treating its own public members (<c>Capacity</c>, the <c>this[]</c> indexer) as properties to map, instead of
/// routing through <c>source.Items.Select(...).ToList()</c>. Fixed in RoyalCode.Extensions.SourceGenerator 0.3.0,
/// where the enumerable resolver resolves the destination's materialization (ToList/ToArray/ToHashSet).
/// </summary>
public class ListDestinationCollectionTests
{
    private const string Fixture =
        """
        using RoyalCode.SmartSelector;
        using System.Collections.Generic;

        namespace Tests.SmartSelector.ListDestination;

        public class OrderItem
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        public class Order
        {
            public int Id { get; set; }
            public List<OrderItem> Items { get; set; } = [];
        }

        [AutoSelect<Order>, AutoProperties]
        public partial class OrderDetails
        {
            public List<OrderItemDetails> Items { get; set; } = [];
        }

        public class OrderItemDetails
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
        """;

    private const string HintName = "Tests.SmartSelector.ListDestination.OrderDetails.AutoSelect.g.cs";

    [Fact]
    public void Select_NestedCollection_DeclaredAsListT_ShouldGenerateSelectToList()
    {
        var result = Util.CompileAndAssert(Fixture);

        var generated = result.GeneratedSource(HintName);
        generated.Should().Contain("Items = a.Items.Select(b => new OrderItemDetails");
        generated.Should().Contain(".ToList()");
        generated.Should().NotContain("Capacity");
        generated.Should().NotContain("this[]");
    }
}
