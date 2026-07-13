using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoPropertiesInformation : IEquatable<AutoPropertiesInformation>
{
    private readonly DiagnosticInfo[]? diagnostics;
    private readonly PropertySnapshot[]? properties;
    private readonly TypeSnapshot? originType;
    private readonly AutoDetailsInformation[]? autoDetails;

    public AutoPropertiesInformation(DiagnosticInfo diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoPropertiesInformation(DiagnosticInfo[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoPropertiesInformation(
        TypeSnapshot originType,
        PropertySnapshot[] properties,
        AutoDetailsInformation[]? autoDetails = null)
    {
        this.originType = originType;
        this.properties = properties;
        this.autoDetails = autoDetails;
    }

    public PropertySnapshot[] Properties => properties ?? [];

    public TypeSnapshot? OriginType => originType;

    public AutoDetailsInformation[] AutoDetails => autoDetails ?? [];

    public bool Equals(AutoPropertiesInformation? other)
    {
        if (other == null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return InformationEquality.SequenceEqual(diagnostics, other.diagnostics) &&
               InformationEquality.SequenceEqual(properties, other.properties) &&
               Equals(originType, other.originType) &&
               InformationEquality.SequenceEqual(autoDetails, other.autoDetails);
    }

    public override bool Equals(object? obj)
    {
        return obj is AutoPropertiesInformation other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 17;
            hashCode = (hashCode * 31) + InformationEquality.SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + InformationEquality.SequenceHashCode(properties);
            hashCode = (hashCode * 31) + (originType?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + InformationEquality.SequenceHashCode(autoDetails);
            return hashCode;
        }
    }

    internal void Generate(SourceProductionContext context)
    {
        if (diagnostics is not null)
        {
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        }

        if (autoDetails is not null)
        {
            foreach (var autoDetail in autoDetails)
            {
                autoDetail.Generate(context);
            }
        }

        if (properties is not null && originType is not null)
        {
            AutoPropertiesGenerator.Generate(this, context);
        }
    }
}
