namespace RoyalCode.SmartSelector.Demo.Tests;

using RoyalCode.SmartSelector.Demo.Details.Library;
using RoyalCode.SmartSelector.Demo.Entities.Library;
using RoyalCode.SmartSelector.Demo.Infra;

public class BookDemoTests
{
    [Fact]
    public void GetSelectExpression()
    {
        // act
        var expression = BookDetails.SelectBookExpression;

        // assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void Create_BookDetails_From_Book()
    {
        // arrange
        var shelf = new Shelf { Location = "A-01" };
        var book = new Book
        {
            Title = "DDD",
            Author = "Eric Evans",
            PublishedDate = new DateTime(2003, 8, 30),
            ISBN = "1234567890",
            Price = 99.90m,
            InStock = true,
            Sku = "SKU-123",
            Shelf = shelf
        };

        // act
        var details = BookDetails.From(book);

        // assert
        Assert.NotNull(details);
        Assert.Equal(book.Id, details.Id);
        Assert.Equal(book.Title, details.Title);
        Assert.Equal(book.Author, details.Author);
        Assert.Equal(book.PublishedDate, details.PublishedDate);
        Assert.Equal(book.ISBN, details.ISBN);
        Assert.Equal(book.Price, details.Price);
        Assert.Equal(book.InStock, details.InStock);
        // Sku excluído (não foi gerado)
        // Propriedade Shelf (aninhada)
        Assert.NotNull(details.Shelf);
        Assert.Equal(book.Shelf.Id, details.Shelf.Id);
        Assert.Equal(book.Shelf.Location, details.Shelf.Location);
    }

    [Fact]
    public void Query_BookDetails_From_Database()
    {
        // arrange
        var db = new AppDbContext();
        db.Database.EnsureCreated();
        var shelf = new Shelf { Location = "B-10" };
        db.Shelves.Add(shelf);
        db.Books.Add(new Book { Title = "Book 1", Author = "Author 1", PublishedDate = DateTime.Today, ISBN = "111", Price = 10m, InStock = true, Shelf = shelf });
        db.Books.Add(new Book { Title = "Book 2", Author = "Author 2", PublishedDate = DateTime.Today, ISBN = "222", Price = 20m, InStock = false, Shelf = shelf });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // act
        var details = db.Books
            .Select(BookDetails.SelectBookExpression)
            .ToList();

        // assert
        Assert.Equal(2, details.Count);
        Assert.Contains(details, d => d.Title == "Book 1" && d.Shelf.Location == "B-10");
        Assert.Contains(details, d => d.Title == "Book 2" && d.Shelf.Location == "B-10");
    }

    [Fact]
    public void Create_ShelfDetails_From_Shelf_Via_BookDetails_Nested()
    {
        // arrange
        var shelf = new Shelf { Location = "C-15" };
        var book = new Book { Title = "Nested", Author = "Author", PublishedDate = DateTime.Today, ISBN = "333", Price = 30m, InStock = true, Shelf = shelf };

        // act
        var details = BookDetails.From(book);

        // assert nested shelf mapping
        Assert.NotNull(details.Shelf);
        Assert.Equal(shelf.Id, details.Shelf.Id);
        Assert.Equal(shelf.Location, details.Shelf.Location);
    }
}
