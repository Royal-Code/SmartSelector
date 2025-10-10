using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoPropertiesInformation : IEquatable<AutoPropertiesInformation>
{
    private readonly Diagnostic[]? diagnostics;
    private readonly PropertyDescriptor[]? properties;
    private readonly TypeDescriptor originType;
    private readonly AutoDetailsInformation[]? autoDetails;

    public AutoPropertiesInformation(Diagnostic diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoPropertiesInformation(Diagnostic[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoPropertiesInformation(
        TypeDescriptor originType, 
        PropertyDescriptor[] properties,
        AutoDetailsInformation[]? autoDetails = null)
    {
        this.originType = originType;
        this.properties = properties;
        this.autoDetails = autoDetails;
    }

    public PropertyDescriptor[] Properties => properties ?? [];

    public TypeDescriptor OriginType => originType;

    public AutoDetailsInformation[] AutoDetails => autoDetails ?? [];

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
               properties?.SequenceEqual(other.properties) == true &&
               autoDetails?.SequenceEqual(other.autoDetails) == true;
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
