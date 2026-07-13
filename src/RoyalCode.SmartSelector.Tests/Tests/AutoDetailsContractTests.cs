using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Tests for the AutoDetails contract (DF2): the type declared on the property
/// is the source of truth for the generated class.
/// </summary>
public class AutoDetailsContractTests
{
    [Fact]
    public void AutoDetails_should_generate_the_type_declared_on_the_property()
    {
        // B2: nome fora da convenção `{Origem}Details` deve gerar exatamente o tipo declarado.
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;
            using System;

            namespace Tests.SmartSelector.Contracts;

            #nullable disable // poco

            public class Address
            {
                public string City { get; set; }
                public string Zip { get; set; }
                public string Street { get; set; }
            }

            public class Customer
            {
                public Guid Id { get; set; }
                public Address Address { get; set; }
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDto Address { get; set; }
                public Guid Id { get; set; }
            }
            """);

        var generated = result.GeneratedSource("Tests.SmartSelector.Contracts.AddressDto.AutoDetails.g.cs");
        generated.Should().Contain("public partial class AddressDto");
        generated.Should().NotContain("AddressDetails");

        var partial = result.GeneratedSource("Tests.SmartSelector.Contracts.CustomerDetails.AutoSelect.g.cs");
        partial.Should().Contain("Address = new AddressDto");
    }

    [Fact]
    public void AutoDetails_should_complete_a_preexisting_partial_type()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;
            using System;

            namespace Tests.SmartSelector.Contracts;

            #nullable disable // poco

            public class Address
            {
                public string City { get; set; }
                public string Zip { get; set; }
                public string Street { get; set; }
            }

            public class Customer
            {
                public Guid Id { get; set; }
                public Address Address { get; set; }
            }

            public partial class AddressDto
            {
                public string City { get; set; }
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDto Address { get; set; }
                public Guid Id { get; set; }
            }
            """);

        var generated = result.GeneratedSource("Tests.SmartSelector.Contracts.AddressDto.AutoDetails.g.cs");
        generated.Should().Contain("public partial class AddressDto");

        // A propriedade já declarada pelo usuário não é gerada novamente.
        generated.Should().NotContain("City");
        generated.Should().Contain("Zip");
        generated.Should().Contain("Street");
    }

    [Fact]
    public void AutoDetails_should_complete_a_preexisting_partial_type_in_the_global_namespace()
    {
        var result = Util.CompileAndAssert(
            """
            using RoyalCode.SmartSelector;

            #nullable disable // poco

            public partial class AddressDto
            {
            }

            namespace Tests.SmartSelector.Contracts
            {
                public class Address
                {
                    public string City { get; set; }
                }

                public class Customer
                {
                    public Address Address { get; set; }
                }

                [AutoSelect<Customer>, AutoProperties]
                public partial class CustomerDetails
                {
                    [AutoDetails]
                    public global::AddressDto Address { get; set; }
                }
            }
            """);

        var generated = result.GeneratedSource("AddressDto.AutoDetails.g.cs");
        generated.Should().Contain("public partial class AddressDto");
        generated.Should().Contain("public string City");
        generated.Should().NotContain("namespace ;");
    }

    [Fact]
    public void RCSS012_should_report_an_existing_non_partial_type()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;
            using System;

            namespace Tests.SmartSelector.Contracts;

            #nullable disable // poco

            public class Address
            {
                public string City { get; set; }
            }

            public class Customer
            {
                public Guid Id { get; set; }
                public Address Address { get; set; }
            }

            public class AddressDto
            {
                public string City { get; set; }
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDto Address { get; set; }
                public Guid Id { get; set; }
            }
            """,
            "RCSS012",
            "Address");
    }

    [Fact]
    public void RCSS013_should_report_duplicated_AutoDetails_generation_for_the_same_type()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;
            using System;

            namespace Tests.SmartSelector.Contracts;

            #nullable disable // poco

            public class Address
            {
                public string City { get; set; }
            }

            public class Customer
            {
                public Guid Id { get; set; }
                public Address Address { get; set; }
                public Address Billing { get; set; }
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDto Address { get; set; }

                [AutoDetails]
                public AddressDto Billing { get; set; }

                public Guid Id { get; set; }
            }
            """,
            "RCSS013",
            "Billing");
    }

    [Fact]
    public void RCSS014_should_report_a_less_accessible_existing_type()
    {
        AssertDiagnostic(
            """
            using RoyalCode.SmartSelector;
            using System;

            namespace Tests.SmartSelector.Contracts;

            #nullable disable // poco

            public class Address
            {
                public string City { get; set; }
            }

            public class Customer
            {
                public Guid Id { get; set; }
                public Address Address { get; set; }
            }

            internal partial class AddressDto
            {
            }

            [AutoSelect<Customer>, AutoProperties]
            public partial class CustomerDetails
            {
                [AutoDetails]
                public AddressDto Address { get; set; }
                public Guid Id { get; set; }
            }
            """,
            "RCSS014",
            "Address");
    }

    private static void AssertDiagnostic(string source, string id, string expectedLocationText)
    {
        var result = Util.Compile(source);
        var diagnostic = result.GeneratorDiagnostics.SingleOrDefault(item => item.Id == id);
        diagnostic.Should().NotBeNull(
            "diagnostic {0} was expected; reported diagnostics:\n{1}",
            id,
            string.Join("\n", result.GeneratorDiagnostics.Concat(result.CompilationDiagnostics)));

        diagnostic.Location.Should().NotBe(Location.None);
        diagnostic.Location.Kind.Should().Be(LocationKind.ExternalFile);
        diagnostic.Location.SourceSpan.Length.Should().BeGreaterThan(0);

        var actualLocationText = source.Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
        actualLocationText.Should().Be(expectedLocationText);
    }
}
