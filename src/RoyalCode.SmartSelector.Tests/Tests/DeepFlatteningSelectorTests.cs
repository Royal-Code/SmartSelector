using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class DeepFlatteningSelectorTests
{
    [Fact]
    public void Select_With_Deep_Flattened_Nested_Properties()
    {
        // arrange + act
        Util.Compile(Code.Types, out var output, out var diagnostics);

        // assert - nenhum erro de geração
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        // classe parcial gerada (expression + From)
        var generatedPartial = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedPartial.Should().Be(Code.ExpectedPartial);

        // classe de extensions
        var generatedExtensions = output.SyntaxTrees.Skip(2).FirstOrDefault()?.ToString();
        generatedExtensions.Should().Be(Code.ExpectedExtension);
    }
}

file static class Code
{
    public const string Types =
"""
using RoyalCode.SmartSelector;
using System;

namespace Tests.SmartSelector.Models;

#nullable disable // poco

public class Region
{
    public string Name { get; set; }
}

public class Country
{
    public string Name { get; set; }
    public string Code { get; set; }
    public Region Region { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public Country Country { get; set; }
}

public class Customer
{
    public Guid Id { get; set; }
    public Address Address { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public Customer Customer { get; set; }
}

// Flatten esperado:
//   CustomerAddressCountryName           -> a.Customer.Address.Country.Name
//   CustomerAddressCountryCode           -> a.Customer.Address.Country.Code
//   CustomerAddressCountryRegionName     -> a.Customer.Address.Country.Region.Name
[AutoSelect<Order>]
public partial class OrderDetails
{
    public Guid Id { get; set; }
    public string CustomerAddressCountryName { get; set; }
    public string CustomerAddressCountryCode { get; set; }
    public string CustomerAddressCountryRegionName { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class OrderDetails
{
    private static Func<Order, OrderDetails> selectOrderFunc;

    public static Expression<Func<Order, OrderDetails>> SelectOrderExpression { get; } = a => new OrderDetails
    {
        Id = a.Id,
        CustomerAddressCountryName = a.Customer.Address.Country.Name,
        CustomerAddressCountryCode = a.Customer.Address.Country.Code,
        CustomerAddressCountryRegionName = a.Customer.Address.Country.Region.Name
    };

    public static OrderDetails From(Order order) => (selectOrderFunc ??= SelectOrderExpression.Compile())(order);
}

""";

    public const string ExpectedExtension =
"""

namespace Tests.SmartSelector.Models;

public static class OrderDetails_Extensions
{
    public static IQueryable<OrderDetails> SelectOrderDetails(this IQueryable<Order> query)
    {
        return query.Select(OrderDetails.SelectOrderExpression);
    }

    public static IEnumerable<OrderDetails> SelectOrderDetails(this IEnumerable<Order> enumerable)
    {
        return enumerable.Select(OrderDetails.From);
    }

    public static OrderDetails ToOrderDetails(this Order order) => OrderDetails.From(order);
}

""";
}
