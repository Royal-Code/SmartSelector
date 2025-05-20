namespace RoyalCode.SmartSelector.Generators.Models.Descriptors;

internal class AssignDescriptor : IEquatable<AssignDescriptor>
{
    public AssignType AssignType { get; set; }

    public bool IsEnumerable { get; set; }

    public bool RequireSelect { get; set; }

    public bool Equals(AssignDescriptor other)
    {
        if (other is null) 
            return false;

        if (ReferenceEquals(this, other)) 
            return true;

        return AssignType == other.AssignType && 
            IsEnumerable == other.IsEnumerable &&
            RequireSelect == other.RequireSelect;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AssignDescriptor other)
            return false;

        return Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = -2066519001;
        hashCode = hashCode * -1521134295 + AssignType.GetHashCode();
        hashCode = hashCode * -1521134295 + IsEnumerable.GetHashCode();
        hashCode = hashCode * -1521134295 + RequireSelect.GetHashCode();
        return hashCode;
    }
}
