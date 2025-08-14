namespace RoyalCode.SmartSelector.Benchmarks.Models;

#nullable disable // base class

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}
