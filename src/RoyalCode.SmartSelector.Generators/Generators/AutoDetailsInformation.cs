using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoDetailsInformation : IEquatable<AutoDetailsInformation>
{
    private readonly Diagnostic[]? diagnostics;

    private readonly string? detailsClassName;
    private readonly AutoPropertiesInformation? autoPropertiesInformation;

    public bool Equals(AutoDetailsInformation other)
    {
        throw new NotImplementedException();
    }
}
