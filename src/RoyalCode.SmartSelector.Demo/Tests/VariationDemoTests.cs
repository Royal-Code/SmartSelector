using RoyalCode.SmartSelector.Demo.Details;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Infra;

namespace RoyalCode.SmartSelector.Demo.Tests;

public class VariationDemoTests
{
    [Fact]
    public void GetSelectExpression()
    {
        // act
        var expression = VariationDetails.SelectVariationExpression;
        // assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void Create_VariationDetails_From_Variation()
    {
        // arrange
        var product = new Product("Product 1");
        var color = new Color("Red", "#FF0000");
        var size = new Size("Large", "L");
        var variation = new Variation(product, "Variation 1", "V1", color, size);

        // act
        var details = VariationDetails.From(variation);

        // assert
        Assert.NotNull(details);
        Assert.Equal(variation.Id, details.Id);
        Assert.Equal(variation.Name, details.Name);
        Assert.Equal(variation.Code, details.Code);
        Assert.Equal(variation.Color.Name, details.ColorName);
        Assert.Equal(variation.Size.Code, details.SizeCode);
    }

    [Fact]
    public void Create_VariationDetails_Using_ToVariationDetails()
    {
        // arrange
        var product = new Product("Product 1");
        var color = new Color("Red", "#FF0000");
        var size = new Size("Large", "L");
        var variation = new Variation(product, "Variation 1", "V1", color, size);

        // act
        var details = variation.ToVariationDetails();

        // assert
        Assert.NotNull(details);
        Assert.Equal(variation.Id, details.Id);
        Assert.Equal(variation.Name, details.Name);
        Assert.Equal(variation.Code, details.Code);
        Assert.Equal(variation.Color.Name, details.ColorName);
        Assert.Equal(variation.Size.Code, details.SizeCode);
    }

    [Fact]
    public void Query_VariationDetails_From_Database()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        var product = new Product("Product 1");
        var color = new Color("Red", "#FF0000");
        var size = new Size("Large", "L");
        db.Products.Add(product);
        db.Colors.Add(color);
        db.Sizes.Add(size);
        db.SaveChanges();
        db.Variations.Add(new Variation(product, "Variation 1", "V1", color, size));
        db.Variations.Add(new Variation(product, "Variation 2", "V2", color, size));
        db.Variations.Add(new Variation(product, "Variation 3", "V3", color, size));
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act
        var details = db.Variations
            .SelectVariationDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Name == "Variation 1");
        Assert.Contains(details, d => d.Name == "Variation 2");
        Assert.Contains(details, d => d.Name == "Variation 3");
    }

    [Fact]
    public void Select_VariationDetails_From_List()
    {
        // arrange
        var product = new Product("Product 1");
        var color = new Color("Red", "#FF0000");
        var size = new Size("Large", "L");
        var variations = new List<Variation>
        {
            new Variation(product, "Variation 1", "V1", color, size),
            new Variation(product, "Variation 2", "V2", color, size),
            new Variation(product, "Variation 3", "V3", color, size)
        };

        // act
        var details = variations
            .SelectVariationDetails()
            .ToList();

        // assert
        Assert.Equal(3, details.Count);
        Assert.Contains(details, d => d.Name == "Variation 1");
        Assert.Contains(details, d => d.Name == "Variation 2");
        Assert.Contains(details, d => d.Name == "Variation 3");
    }
}
