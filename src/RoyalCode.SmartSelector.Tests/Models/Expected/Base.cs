namespace RoyalCode.SmartSelector.Tests.Models.Expected;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}
