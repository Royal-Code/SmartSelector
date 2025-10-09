using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class FlatteningSelectorTests
{
    [Fact]
    public void Select_With_Flattened_Nested_Properties()
    {
        // arrange + act
        Util.Compile(Code.Types, out var output, out var diagnostics);

        // assert - sem erros de compilação do gerador
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        // primeira árvore gerada: classe parcial com expressão + From
        var generatedPartial = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedPartial.Should().Be(Code.ExpectedPartial);

        // segunda árvore gerada: classe de extensions
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

public class Address
{
    public string City { get; set; }
    public string Zip  { get; set; }
}

public class Customer
{
    public Guid Id { get; set; }
    public Address Address { get; set; }
}

// Propriedades AddressCity e AddressZip devem ser casadas com Address.City e Address.Zip
[AutoSelect<Customer>]
public partial class CustomerDetails
{
    public Guid Id { get; set; }
    public string AddressCity { get; set; }
    public string AddressZip { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class CustomerDetails
{
    private static Func<Customer, CustomerDetails> selectCustomerFunc;

    public static Expression<Func<Customer, CustomerDetails>> SelectCustomerExpression { get; } = a => new CustomerDetails
    {
        Id = a.Id,
        AddressCity = a.Address.City,
        AddressZip = a.Address.Zip
    };

    public static CustomerDetails From(Customer customer) => (selectCustomerFunc ??= SelectCustomerExpression.Compile())(customer);
}

""";

    public const string ExpectedExtension =
"""

namespace Tests.SmartSelector.Models;

public static class CustomerDetails_Extensions
{
    public static IQueryable<CustomerDetails> SelectCustomerDetails(this IQueryable<Customer> query)
    {
        return query.Select(CustomerDetails.SelectCustomerExpression);
    }

    public static IEnumerable<CustomerDetails> SelectCustomerDetails(this IEnumerable<Customer> enumerable)
    {
        return enumerable.Select(CustomerDetails.From);
    }

    public static CustomerDetails ToCustomerDetails(this Customer customer) => CustomerDetails.From(customer);
}

""";
}
