using RoyalCode.SmartSelector.Demo.Details;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Infra;

namespace RoyalCode.SmartSelector.Demo.Samples;

public class ProductDemoTests
{
    [Fact]
    public void GetSelectExpression()
    {
        // act
        var expression = ProductDetails.SelectProductExpression;

        // assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void Create_ProductDetails_From_Product()
    {
        // arrange
        var product = new Product("Hot dog 1");

        // act
        var details = ProductDetails.From(product);

        // assert
        Assert.NotNull(details);
        Assert.Equal(product.Id, details.Id);
        Assert.Equal(product.Name, details.Name);
        Assert.Equal(product.IsActive, details.IsActive);
    }

    [Fact]
    public void Create_ProductDetails_Using_ToProductDetails()
    {
        // arrange
        var product = new Product("Hot dog 1");

        // act
        var details = product.ToProductDetails();

        // assert
        Assert.NotNull(details);
        Assert.Equal(product.Id, details.Id);
        Assert.Equal(product.Name, details.Name);
        Assert.Equal(product.IsActive, details.IsActive);
    }

    [Fact]
    public void Query_ProductDetails_From_Database()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        db.Products.Add(new Product("Hot dog 1"));
        db.Products.Add(new Product("Hot dog 2"));
        db.Products.Add(new Product("Hot dog 3"));
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act
        var details = db.Products
            .SelectProductDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Name == "Hot dog 1");
        Assert.Contains(details, d => d.Name == "Hot dog 2");
        Assert.Contains(details, d => d.Name == "Hot dog 3");
    }

    [Fact]
    public void Select_ProductDetails_From_List()
    {
        // arrange
        var products = new List<Product>
        {
            new Product("Hot dog 1"),
            new Product("Hot dog 2"),
            new Product("Hot dog 3"),
        };

        // act
        var details = products
            .SelectProductDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Name == "Hot dog 1");
        Assert.Contains(details, d => d.Name == "Hot dog 2");
        Assert.Contains(details, d => d.Name == "Hot dog 3");
    }
}
