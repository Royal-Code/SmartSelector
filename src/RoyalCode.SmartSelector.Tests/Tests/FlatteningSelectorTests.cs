using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class FlatteningSelectorTests
{
    [Fact]
    public void Select_With_Flattened_Nested_Properties()
    {
        var result = Util.CompileAndAssert(Code.Types);

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
        Id = a.Id,
        AddressCity = a.Address.City,
        AddressZip = a.Address.Zip
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
