using RoyalCode.SmartSelector.Generators.Models.Descriptors;

namespace RoyalCode.SmartSelector.Generators.Models;

/// <summary>
/// A result of matching a property from the origin type to a property in the target type.
/// </summary>
internal class PropertyMatch(PropertyDescriptor origin, PropertySelection? target) : IEquatable<PropertyMatch>
{
    /// <summary>
    /// The origin property type descriptor.
    /// </summary>
    public PropertyDescriptor Origin { get; } = origin;

    /// <summary>
    /// The target property selection.
    /// </summary>
    public PropertySelection? Target { get; } = target;

    /// <summary>
    /// Determines if the target property selection is missing.
    /// </summary>
    public bool IsMissing => Target is null;

    public bool Equals(PropertyMatch other)
    {
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;

        return Origin.Equals(other.Origin) &&
            Equals(Target, other.Target);
    }

    public override bool Equals(object? obj)
    {
        return obj is PropertyMatch other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = -1013312977;
        hashCode = hashCode * -1521134295 + EqualityComparer<PropertyDescriptor>.Default.GetHashCode(Origin);
        hashCode = hashCode * -1521134295 + EqualityComparer<PropertySelection?>.Default.GetHashCode(Target);
        return hashCode;
    }
}
