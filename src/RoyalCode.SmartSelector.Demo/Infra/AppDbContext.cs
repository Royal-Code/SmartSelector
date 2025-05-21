using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoyalCode.SmartSelector.Demo.Entities;

namespace RoyalCode.SmartSelector.Demo.Infra;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = default!;

    public DbSet<User> Users { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<User>().ToTable("Users");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use SQLite in-memory database for testing purposes
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        optionsBuilder.UseSqlite(conn);
    }
}
