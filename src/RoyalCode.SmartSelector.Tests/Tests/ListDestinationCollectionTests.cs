using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Reproduces a generator bug found via RoyalCode.SmartCommands.Demo (see that repo's
/// <c>.docs/plans/plan-demo-usage-scenarios.md</c>, "Registro de gaps"): when a DTO declares a nested object
/// collection using the concrete <c>List&lt;T&gt;</c> type (instead of <c>IReadOnlyList&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>,
/// as every other nested-collection test in this suite does), the generator emits an invalid object initializer
/// for the destination <c>List&lt;T&gt;</c> itself. It treats <c>List&lt;T&gt;</c>'s own public members
/// (<c>Capacity</c>, the <c>this[]</c> indexer) as if they were properties to map from the source collection,
/// instead of routing through the normal <c>source.Items.Select(...).ToList()</c> path. The emitted code does not
/// compile (CS0443/CS1001/CS1003 on the invalid <c>this[] = ...</c> member).
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

    [Fact(Skip =
        "Known generator bug: a nested-collection destination declared as List<T> emits an invalid object " +
        "initializer (treats List<T>.Capacity/this[] as mappable members) instead of Select(...).ToList(). " +
        "Workaround used in RoyalCode.SmartCommands.Demo: declare the DTO collection as IReadOnlyList<T>/" +
        "ICollection<T> instead of List<T>. Remove this Skip once the generator emits the correct code; the " +
        "companion test below stays green while the bug is present and will start failing once it is fixed, " +
        "signaling that this Skip should come off.")]
    public void Select_NestedCollection_DeclaredAsListT_ShouldGenerateSelectToList()
    {
        var result = Util.CompileAndAssert(Fixture);

        var generated = result.GeneratedSource(HintName);
        generated.Should().Contain("Items = a.Items.Select(b => new OrderItemDetails");
        generated.Should().Contain(".ToList()");
        generated.Should().NotContain("Capacity");
        generated.Should().NotContain("this[]");
    }

    [Fact]
    public void Select_NestedCollection_DeclaredAsListT_CurrentlyProducesInvalidCode()
    {
        // Canary: documents today's (buggy) behavior via CompileFast, which does not assert success on its own.
        // Stays green while the bug is present. Once the generator is fixed, the assertions below will fail,
        // signaling that Select_NestedCollection_DeclaredAsListT_ShouldGenerateSelectToList's [Skip] should be
        // removed (that test encodes the correct, fixed behavior).
        var result = Util.CompileFast(Fixture);

        result.Errors.Should().NotBeEmpty(
            "the generator currently emits an invalid initializer for List<T> nested-collection destinations");

        var generated = result.GeneratedSource(HintName);
        generated.Should().Contain("Capacity = a.Items.Capacity");
        generated.Should().Contain("this[] = new OrderItemDetails");
    }
}
