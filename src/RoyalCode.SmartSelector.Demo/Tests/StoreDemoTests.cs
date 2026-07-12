namespace RoyalCode.SmartSelector.Demo.Tests;

using RoyalCode.SmartSelector.Demo.Details.Store;
using RoyalCode.SmartSelector.Demo.Entities.Store;
using RoyalCode.SmartSelector.Demo.Infra;

/// <summary>
/// Testes da política de null (DF5/DF18): propagação de null para destino anulável e
/// coleção vazia para destino não anulável, em memória e traduzido pelo EF Core (SQLite).
/// </summary>
public class StoreDemoTests
{
    [Fact]
    public void NullPolicy_From_With_Null_Navigation_And_Collection()
    {
        // arrange: fornecedor sem depósito e sem contatos
        var supplier = new Supplier { Name = "Acme" };

        // act
        var details = SupplierDetails.From(supplier);

        // assert: navegação anulável propaga null; coleção não anulável vira vazia
        Assert.NotNull(details);
        Assert.Equal("Acme", details.Name);
        Assert.Null(details.Warehouse);
        Assert.NotNull(details.Contacts);
        Assert.Empty(details.Contacts);
    }

    [Fact]
    public void NullPolicy_From_With_Complete_Graph()
    {
        // arrange
        var supplier = new Supplier
        {
            Name = "Globex",
            Warehouse = new Warehouse { Location = "Dock 42" },
            Contacts = [new Contact { Email = "a@globex.test" }, new Contact { Email = "b@globex.test" }],
        };

        // act
        var details = SupplierDetails.From(supplier);

        // assert
        Assert.NotNull(details.Warehouse);
        Assert.Equal("Dock 42", details.Warehouse!.Location);
        Assert.Equal(2, details.Contacts.Count);
    }

    [Fact]
    public void NullPolicy_Query_Should_Translate_With_EFCore_Sqlite()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        db.Suppliers.Add(new Supplier
        {
            Name = "Globex",
            Warehouse = new Warehouse { Location = "Dock 42" },
            Contacts = [new Contact { Email = "a@globex.test" }],
        });
        db.Suppliers.Add(new Supplier { Name = "Acme" });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act: as condicionais de null precisam ser traduzíveis pelo provedor
        var details = db.Suppliers
            .Select(SupplierDetails.SelectSupplierExpression)
            .OrderBy(d => d.Name)
            .ToList();

        // assert
        Assert.Equal(2, details.Count);

        var acme = details[0];
        Assert.Equal("Acme", acme.Name);
        Assert.Null(acme.Warehouse);
        Assert.NotNull(acme.Contacts);
        Assert.Empty(acme.Contacts);

        var globex = details[1];
        Assert.Equal("Globex", globex.Name);
        Assert.NotNull(globex.Warehouse);
        Assert.Equal("Dock 42", globex.Warehouse!.Location);
        Assert.Single(globex.Contacts);
        Assert.Equal("a@globex.test", globex.Contacts[0].Email);
    }
}
