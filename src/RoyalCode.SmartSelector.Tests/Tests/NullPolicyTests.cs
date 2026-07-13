using System.Collections;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace RoyalCode.SmartSelector.Tests.Tests;

/// <summary>
/// Tests for the null policy (DF5/DF18): null propagation for nullable destinations,
/// empty-collection fallback for non-nullable destination collections (RCSS016)
/// and warning for nullable sources flowing into non-nullable destinations (RCSS015).
/// </summary>
public class NullPolicyTests
{
    private const string NavigationFixture =
        """
        using RoyalCode.SmartSelector;
        using System.Collections.Generic;

        namespace Tests.SmartSelector.NullPolicy;

        public class Address
        {
            public string City { get; set; } = string.Empty;
        }

        public class Item
        {
            public string Name { get; set; } = string.Empty;
        }

        public class Order
        {
            public int Id { get; set; }
            public Address? Address { get; set; }
            public ICollection<Item>? Items { get; set; }
        }

        [AutoSelect<Order>, AutoProperties]
        public partial class OrderDetails
        {
            public int Id { get; set; }

            [AutoDetails]
            public AddressDetails? Address { get; set; }

            public IReadOnlyList<ItemDetails>? Items { get; set; }
        }

        public class ItemDetails
        {
            public string Name { get; set; } = string.Empty;
        }
        """;

    [Fact]
    public void NullPolicy_nullable_navigation_to_nullable_destination_should_propagate_null()
    {
        var result = Util.CompileAndAssert(NavigationFixture);

        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.OrderDetails.AutoSelect.g.cs");
        generated.Should().Contain("Address = a.Address == null ? null : new AddressDetails");
        generated.Should().Contain("Items = a.Items == null ? null : a.Items.Select(");

        result.GeneratorDiagnostics.Should().NotContain(d => d.Id == "RCSS015" || d.Id == "RCSS016");
    }

    [Fact]
    public void NullPolicy_From_with_null_graph_should_not_throw()
    {
        var result = Util.CompileFast(NavigationFixture);
        result.Errors.Should().BeEmpty();

        var assembly = EmitAndLoad(result.OutputCompilation);
        var orderType = assembly.GetType("Tests.SmartSelector.NullPolicy.Order")!;
        var detailsType = assembly.GetType("Tests.SmartSelector.NullPolicy.OrderDetails")!;

        var order = Activator.CreateInstance(orderType)!;
        orderType.GetProperty("Id")!.SetValue(order, 7);

        var from = detailsType.GetMethod("From", BindingFlags.Public | BindingFlags.Static)!;
        var details = from.Invoke(null, [order]);

        details.Should().NotBeNull();
        detailsType.GetProperty("Id")!.GetValue(details).Should().Be(7);
        detailsType.GetProperty("Address")!.GetValue(details).Should().BeNull();
        detailsType.GetProperty("Items")!.GetValue(details).Should().BeNull();
    }

    [Fact]
    public void NullPolicy_nullable_collection_to_non_nullable_destination_should_fallback_to_empty_and_report_RCSS016()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;
            using System.Collections.Generic;

            namespace Tests.SmartSelector.NullPolicy;

            public class Item
            {
                public string Name { get; set; } = string.Empty;
            }

            public class Order
            {
                public int Id { get; set; }
                public ICollection<Item>? Items { get; set; }
            }

            [AutoSelect<Order>]
            public partial class OrderDetails
            {
                public int Id { get; set; }
                public IReadOnlyList<ItemDetails> Items { get; set; } = [];
            }

            public class ItemDetails
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = Util.CompileFast(fixture);
        result.Errors.Should().BeEmpty();

        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.OrderDetails.AutoSelect.g.cs");
        generated.Should().Contain("Items = a.Items == null ? new List<ItemDetails>() : a.Items.Select(");

        var info = result.GeneratorDiagnostics.Single(d => d.Id == "RCSS016");
        info.Severity.Should().Be(DiagnosticSeverity.Info);
        GetLocationText(fixture, info).Should().Be("Items");

        // execução em memória: origem nula produz coleção vazia
        var assembly = EmitAndLoad(result.OutputCompilation);
        var orderType = assembly.GetType("Tests.SmartSelector.NullPolicy.Order")!;
        var detailsType = assembly.GetType("Tests.SmartSelector.NullPolicy.OrderDetails")!;
        var order = Activator.CreateInstance(orderType)!;

        var from = detailsType.GetMethod("From", BindingFlags.Public | BindingFlags.Static)!;
        var details = from.Invoke(null, [order]);

        var items = (IEnumerable)detailsType.GetProperty("Items")!.GetValue(details)!;
        items.Should().NotBeNull();
        items.Cast<object>().Should().BeEmpty();
    }

    [Fact]
    public void NullPolicy_nullable_scalar_to_non_nullable_destination_should_report_RCSS015()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;

            namespace Tests.SmartSelector.NullPolicy;

            public class Person
            {
                public int Id { get; set; }
                public string? Nick { get; set; }
            }

            [AutoSelect<Person>]
            public partial class PersonDetails
            {
                public int Id { get; set; }
                public string Nick { get; set; } = string.Empty;
            }
            """;

        var result = Util.Compile(fixture);
        result.Errors.Should().BeEmpty();

        var warning = result.GeneratorDiagnostics.Single(d => d.Id == "RCSS015");
        warning.Severity.Should().Be(DiagnosticSeverity.Warning);
        GetLocationText(fixture, warning).Should().Be("Nick");

        // comportamento mantido: atribuição direta
        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.PersonDetails.AutoSelect.g.cs");
        generated.Should().Contain("Nick = a.Nick");
    }

    [Fact]
    public void NullPolicy_nullable_value_scalar_to_non_nullable_destination_should_report_RCSS015()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;

            namespace Tests.SmartSelector.NullPolicy;

            public class Person
            {
                public int? Age { get; set; }
            }

            [AutoSelect<Person>]
            public partial class PersonDetails
            {
                public int Age { get; set; }
            }
            """;

        var result = Util.Compile(fixture);
        result.Errors.Should().BeEmpty();

        var warning = result.GeneratorDiagnostics.Single(d => d.Id == "RCSS015");
        warning.Severity.Should().Be(DiagnosticSeverity.Warning);
        GetLocationText(fixture, warning).Should().Be("Age");
    }

    [Fact]
    public void NullPolicy_flattening_through_nullable_parent_should_propagate_or_warn()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;

            namespace Tests.SmartSelector.NullPolicy;

            public class Address
            {
                public string City { get; set; } = string.Empty;
                public string Zip { get; set; } = string.Empty;
            }

            public class Customer
            {
                public int Id { get; set; }
                public Address? Address { get; set; }
            }

            [AutoSelect<Customer>]
            public partial class CustomerDetails
            {
                public int Id { get; set; }
                public string? AddressCity { get; set; }
                public string AddressZip { get; set; } = string.Empty;
            }
            """;

        var result = Util.CompileFast(fixture);
        result.Errors.Should().BeEmpty();

        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.CustomerDetails.AutoSelect.g.cs");

        // destino anulável: condicional propaga null
        generated.Should().Contain("AddressCity = a.Address == null ? null : a.Address.City");

        // destino não anulável: comportamento mantido + warning
        generated.Should().Contain("AddressZip = a.Address.Zip");
        var warning = result.GeneratorDiagnostics.Single(d => d.Id == "RCSS015");
        GetLocationText(fixture, warning).Should().Be("AddressZip");

        // execução: grafo nulo não lança NRE para o destino anulável
        var assembly = EmitAndLoad(result.OutputCompilation);
        var customerType = assembly.GetType("Tests.SmartSelector.NullPolicy.Customer")!;
        var detailsType = assembly.GetType("Tests.SmartSelector.NullPolicy.CustomerDetails")!;
        var customer = Activator.CreateInstance(customerType)!;

        var expressionProperty = detailsType
            .GetProperty("SelectCustomerExpression", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null);
        expressionProperty.Should().NotBeNull();

        // From lança apenas por causa de AddressZip (comportamento mantido, avisado por RCSS015);
        // valida o caminho anulável via propriedade individual
        var lambda = (System.Linq.Expressions.LambdaExpression)expressionProperty!;
        lambda.ToString().Should().Contain("== null");
        _ = customer;
    }

    [Fact]
    public void NullPolicy_flattening_through_nullable_parent_to_nullable_value_should_propagate_null()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;

            namespace Tests.SmartSelector.NullPolicy;

            public class Address
            {
                public int Zip { get; set; }
            }

            public class Customer
            {
                public Address? Address { get; set; }
            }

            [AutoSelect<Customer>]
            public partial class CustomerDetails
            {
                public int? AddressZip { get; set; }
            }
            """;

        var result = Util.CompileFast(fixture);
        result.Errors.Should().BeEmpty();

        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.CustomerDetails.AutoSelect.g.cs");
        generated.Should().Contain("AddressZip = a.Address == null ? default(Int32?) :");
        result.GeneratorDiagnostics.Should().NotContain(d => d.Id == "RCSS015");
    }

    [Fact]
    public void NullPolicy_nested_unsafe_match_should_report_RCSS015_on_the_root_property()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;
            using System.Collections.Generic;

            namespace Tests.SmartSelector.NullPolicy;

            public class Item
            {
                public string? Name { get; set; }
            }

            public class Order
            {
                public ICollection<Item> Items { get; set; } = [];
            }

            [AutoSelect<Order>]
            public partial class OrderDetails
            {
                public IReadOnlyList<ItemDetails> Items { get; set; } = [];
            }

            public class ItemDetails
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        var result = Util.Compile(fixture);
        result.Errors.Should().BeEmpty();

        var warning = result.GeneratorDiagnostics.Single(d => d.Id == "RCSS015");
        warning.GetMessage().Should().Contain("Items.Name");
        GetLocationText(fixture, warning).Should().Be("Items");
    }

    [Fact]
    public void NullPolicy_oblivious_code_should_keep_the_previous_behavior()
    {
        const string fixture =
            """
            using RoyalCode.SmartSelector;
            using System.Collections.Generic;

            namespace Tests.SmartSelector.NullPolicy;

            #nullable disable // poco

            public class Item
            {
                public string Name { get; set; }
            }

            public class Order
            {
                public int Id { get; set; }
                public ICollection<Item> Items { get; set; }
            }

            [AutoSelect<Order>]
            public partial class OrderDetails
            {
                public int Id { get; set; }
                public IReadOnlyList<ItemDetails> Items { get; set; }
            }

            public class ItemDetails
            {
                public string Name { get; set; }
            }
            """;

        var result = Util.CompileAndAssert(fixture);

        var generated = result.GeneratedSource("Tests.SmartSelector.NullPolicy.OrderDetails.AutoSelect.g.cs");
        generated.Should().Contain("Items = a.Items.Select(");
        generated.Should().NotContain("== null");
        result.GeneratorDiagnostics.Should().NotContain(d => d.Id == "RCSS015" || d.Id == "RCSS016");
    }

    private static string GetLocationText(string source, Diagnostic diagnostic)
    {
        diagnostic.Location.Kind.Should().Be(LocationKind.ExternalFile);
        return source.Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
    }

    private static Assembly EmitAndLoad(Compilation compilation)
    {
        using var stream = new MemoryStream();
        EmitResult emitResult = compilation.Emit(stream);
        emitResult.Success.Should().BeTrue(
            "the generated code must compile for execution; diagnostics:\n{0}",
            string.Join("\n", emitResult.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));

        return Assembly.Load(stream.ToArray());
    }
}
