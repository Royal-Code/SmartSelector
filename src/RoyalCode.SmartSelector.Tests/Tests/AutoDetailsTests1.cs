using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoDetailsTests1
{
    [Fact]
    public void Generate_Details_Class_For_Nested_Property_With_AutoDetails()
    {
        // arrange + act
        Util.Compile(Code.Types, out var output, out var diagnostics);

        // assert - sem erros de compilação
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        // árvores geradas
        // 0 -> código original
        // 1 -> classe de detalhes do Address (gerada pelo AutoDetails)
        // 2 -> classe parcial principal (AutoSelect + expressão + From)
        // 3 -> extensions
        var generatedDetails = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedDetails.Should().Be(Code.ExpectedDetailsClass);

        var generatedPartial = output.SyntaxTrees.Skip(2).FirstOrDefault()?.ToString();
        generatedPartial.Should().Be(Code.ExpectedPartial);

        var generatedExtensions = output.SyntaxTrees.Skip(3).FirstOrDefault()?.ToString();
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
    public string Street { get; set; }
}

public class Customer
{
    public Guid Id { get; set; }
    public Address Address { get; set; }
}

// Propriedade Address decorada com AutoDetails deve gerar classe AddressDetails com as propriedades simples de Address
[AutoSelect<Customer>, AutoProperties]
public partial class CustomerDetails
{
    [AutoDetails]
    public AddressDetails Address { get; set; }
    public Guid Id { get; set; }
}
""";

    public const string ExpectedDetailsClass =
    """

namespace Tests.SmartSelector.Models;

public class AddressDetails
{
    public string City { get; set; }

    public string Zip { get; set; }

    public string Street { get; set; }
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
        Address = new AddressDetails
        {
            City = a.Address.City,
            Zip = a.Address.Zip,
            Street = a.Address.Street
        },
        Id = a.Id
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
