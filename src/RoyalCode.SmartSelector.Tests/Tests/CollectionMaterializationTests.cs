using FluentAssertions;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Cobre a materialização de coleções aninhadas conforme o tipo declarado no destino, delegada ao
/// <c>EnumerableAssignDescriptorResolver</c> a partir do RoyalCode.Extensions.SourceGenerator 0.3.0:
/// <c>List&lt;T&gt;</c> e afins usam <c>ToList()</c>, <c>T[]</c> usa <c>ToArray()</c> e <c>HashSet&lt;T&gt;</c>
/// usa <c>ToHashSet()</c>. Também cobre elementos que só precisam de conversão (enums equivalentes), caso em
/// que o lambda do <c>Select</c> não projeta um objeto novo, apenas converte o elemento.
/// </summary>
public class CollectionMaterializationTests
{
    [Fact]
    public void Select_NestedCollection_DeclaredAsHashSet_ShouldGenerateSelectToHashSet()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;
            using System.Collections.Generic;

            namespace Tests.SmartSelector.Materialization;

            public class OrderItem
            {
                public string ProductName { get; set; } = string.Empty;
            }

            public class Order
            {
                public List<OrderItem> Items { get; set; } = [];
            }

            [AutoSelect<Order>, AutoProperties]
            public partial class OrderDetails
            {
                public HashSet<OrderItemDetails> Items { get; set; } = [];
            }

            public class OrderItemDetails
            {
                public string ProductName { get; set; } = string.Empty;
            }
            """);

        var generated = result.GeneratedSource(
            "Tests.SmartSelector.Materialization.OrderDetails.AutoSelect.g.cs");

        generated.Should().Contain("Items = a.Items.Select(b => new OrderItemDetails");
        generated.Should().Contain(".ToHashSet()");
    }

    /// <summary>
    /// Uma coleção de enums equivalentes resolve o elemento como um cast, sem seleção interna. Antes, isso
    /// derrubava o gerador (<c>ArgumentException: Inner selection is null</c>), porque o assignment do elemento
    /// era descartado; agora ele vem no <c>ElementAssignment</c> e o lambda emite apenas a conversão.
    /// </summary>
    [Fact]
    public void Select_NestedCollection_OfEquivalentEnums_ShouldGenerateSelectWithCast()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;
            using System.Collections.Generic;

            namespace Tests.SmartSelector.Materialization.Enums;

            public enum Status { Draft, Done }

            public enum StatusDto { Draft, Done }

            public class Order
            {
                public List<Status> Status { get; set; } = [];
            }

            [AutoSelect<Order>, AutoProperties]
            public partial class OrderDetails
            {
                public List<StatusDto> Status { get; set; } = [];
            }
            """);

        var generated = result.GeneratedSource(
            "Tests.SmartSelector.Materialization.Enums.OrderDetails.AutoSelect.g.cs");

        generated.Should().Contain("Status = a.Status.Select(b => (StatusDto)b).ToList()");
    }
}
