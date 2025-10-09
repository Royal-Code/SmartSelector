using Microsoft.CodeAnalysis;
using RoyalCode.Extensions.SourceGenerator.Descriptors.PropertySelection;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoPropertiesInformation : IEquatable<AutoPropertiesInformation>
{
    private readonly Diagnostic[]? diagnostics;
    private readonly PropertyDescriptor[]? properties;
    private readonly TypeDescriptor originType;

    public AutoPropertiesInformation(Diagnostic diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoPropertiesInformation(Diagnostic[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoPropertiesInformation(TypeDescriptor originType, PropertyDescriptor[] properties)
    {
        this.originType = originType;
        this.properties = properties;
    }

    public PropertyDescriptor[] Properties => properties ?? [];

    public TypeDescriptor OriginType => originType;

    public bool Equals(AutoPropertiesInformation other)
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

    internal void Generate(SourceProductionContext context)
    {
        if (diagnostics is not null)
        {
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        if (properties is not null && originType is not null)
        {
            AutoPropertiesGenerator.Generate(this, context);
        }
    }
}
