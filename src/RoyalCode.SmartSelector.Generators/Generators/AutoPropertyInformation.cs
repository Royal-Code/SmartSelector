using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoPropertyInformation : IEquatable<AutoPropertyInformation>
{
    private readonly Diagnostic[]? diagnostics;
    private readonly PropertyDescriptor[]? properties;
    private readonly TypeDescriptor originType;

    public AutoPropertyInformation(Diagnostic diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoPropertyInformation(Diagnostic[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoPropertyInformation(TypeDescriptor originType, PropertyDescriptor[] properties)
    {
        this.originType = originType;
        this.properties = properties;
    }

    public PropertyDescriptor[] Properties => properties ?? [];

    public TypeDescriptor OriginType => originType;

    public bool Equals(AutoPropertyInformation other)
    {
        if (other == null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return diagnostics?.SequenceEqual(other.diagnostics) == true &&
               properties?.SequenceEqual(other.properties) == true;
    }
}
