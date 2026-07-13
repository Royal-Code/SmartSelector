using RoyalCode.SmartSelector.Demo.Details;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Entities.Store;
using RoyalCode.SmartSelector.Demo.Infra;

namespace RoyalCode.SmartSelector.Demo.Tests;

public class MappingFeatureDemoTests
{
    [Fact]
    public void Direct_AutoSelect_configuration_should_translate_with_EFCore()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
        db.Products.Add(new Product("Configured"));
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var value = db.Products
            .Select(AutoConfiguredProductDetails.SelectProductExpression)
            .Single(product => product.Name == "Configured");

        Assert.Equal("Configured", value.Name);
        Assert.True(value.IsActive);
    }

    [Fact]
    public void Nested_MapFrom_should_translate_with_EFCore()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
        db.Suppliers.Add(new Supplier
        {
            Name = "Nested source",
            Warehouse = new Warehouse { Location = "Dock 7" },
        });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var value = db.Suppliers
            .Select(SupplierWarehouseLocationDetails.SelectSupplierExpression)
            .Single();

        Assert.Equal("Dock 7", value.Location);
    }
}
