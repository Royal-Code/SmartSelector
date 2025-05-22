using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoyalCode.SmartSelector.Demo.Entities;
using RoyalCode.SmartSelector.Demo.Entities.Blogs;

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
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use SQLite in-memory database for testing purposes
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        optionsBuilder.UseSqlite(conn);
    }
}
