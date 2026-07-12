using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Entities.Blogs;
using RoyalCode.SmartSelector.Demo.Entities.Library; // added for Book & Shelf
using RoyalCode.SmartSelector.Demo.Entities.Store;

namespace RoyalCode.SmartSelector.Demo.Infra;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = default!;

    public DbSet<Color> Colors { get; set; } = default!;

    public DbSet<Size> Sizes { get; set; } = default!;

    public DbSet<Variation> Variations { get; set; } = default!;

    public DbSet<User> Users { get; set; } = default!;

    public DbSet<Blog> Blogs { get; set; } = default!;

    public DbSet<Post> Posts { get; set; } = default!;

    public DbSet<Comment> Comments { get; set; } = default!;

    public DbSet<Author> Authors { get; set; } = default!;

    public DbSet<Book> Books { get; set; } = default!;

    public DbSet<Shelf> Shelves { get; set; } = default!;

    public DbSet<Supplier> Suppliers { get; set; } = default!;

    public DbSet<Warehouse> Warehouses { get; set; } = default!;

    public DbSet<Contact> Contacts { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<Color>().ToTable("Colors");
        modelBuilder.Entity<Size>().ToTable("Sizes");
        modelBuilder.Entity<Variation>().ToTable("Variations");
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Blog>().ToTable("Blogs");
        modelBuilder.Entity<Post>().ToTable("Posts");
        modelBuilder.Entity<Comment>().ToTable("Comments");
        modelBuilder.Entity<Author>().ToTable("Authors");
        modelBuilder.Entity<Book>().ToTable("Books");
        modelBuilder.Entity<Shelf>().ToTable("Shelves");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<Warehouse>().ToTable("Warehouses");
        modelBuilder.Entity<Contact>().ToTable("Contacts");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use SQLite in-memory database for testing purposes
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        optionsBuilder.UseSqlite(conn);
    }
}
