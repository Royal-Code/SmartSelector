using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class AutoDetailsTests1
{
    [Fact]
    public void Generate_Details_Class_For_Nested_Property_With_AutoDetails()
    {
        var result = Util.CompileAndAssert(Code.Types);

        var generatedDetails = result.GeneratedSource("Tests.SmartSelector.Models.AddressDetails.AutoDetails.g.cs");
        generatedDetails.Should().Be(Code.ExpectedDetailsClass);

        var generatedPartial = result.GeneratedSource("Tests.SmartSelector.Models.CustomerDetails.AutoSelect.g.cs");
        generatedPartial.Should().Be(Code.ExpectedPartial);

        var generatedExtensions = result.GeneratedSource("Tests.SmartSelector.Models.CustomerDetails.Extensions.g.cs");
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
using System;
using System.Linq;
using System.Collections.Generic;

namespace Tests.SmartSelector.Models;

public partial class AddressDetails
{
    public string City { get; set; }

    public string Zip { get; set; }

    public string Street { get; set; }
}

""";
    public const string ExpectedPartial =
    """
using System;
using System.Linq;
using System.Collections.Generic;
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
using System;
using System.Linq;
using System.Collections.Generic;

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
