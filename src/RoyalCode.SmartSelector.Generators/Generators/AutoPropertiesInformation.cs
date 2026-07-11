using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoPropertiesInformation : IEquatable<AutoPropertiesInformation>
{
    private readonly Diagnostic[]? diagnostics;
    private readonly PropertyDescriptor[]? properties;
    private readonly TypeDescriptor? originType;
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

    public TypeDescriptor? OriginType => originType;

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
        return SequenceEqual(diagnostics, other.diagnostics) &&
               SequenceEqual(properties, other.properties) &&
               Equals(originType, other.originType) &&
               SequenceEqual(autoDetails, other.autoDetails);
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
            hashCode = (hashCode * 31) + SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + SequenceHashCode(properties);
            hashCode = (hashCode * 31) + (originType?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + SequenceHashCode(autoDetails);
            return hashCode;
        }
    }

    private static bool SequenceEqual<T>(T[]? left, T[]? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        return left is not null && right is not null && left.SequenceEqual(right);
    }

    private static int SequenceHashCode<T>(T[]? values)
    {
        if (values is null)
            return 0;

        unchecked
        {
            var hashCode = 17;
            foreach (var value in values)
                hashCode = (hashCode * 31) + (value?.GetHashCode() ?? 0);

            return hashCode;
        }
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
