using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoDetailsInformation : IEquatable<AutoDetailsInformation>
{
    private readonly DiagnosticInfo[]? diagnostics;

    private readonly string? detailsClassName;
    private readonly AutoPropertiesInformation? autoPropertiesInformation;

    public AutoDetailsInformation(DiagnosticInfo diagnostic, string? propertyName = null)
    {
        diagnostics = [diagnostic];
        PropertyName = propertyName;
    }

    public AutoDetailsInformation(
        string detailsClassName,
        AutoPropertiesInformation autoPropertiesInformation)
    {
        this.detailsClassName = detailsClassName;
        this.autoPropertiesInformation = autoPropertiesInformation;
    }

    /// <summary>
    /// Nome da propriedade com AutoDetails que originou os diagnósticos, quando houver.
    /// </summary>
    internal string? PropertyName { get; }

    internal DiagnosticInfo[]? Diagnostics => diagnostics;

    public bool Equals(AutoDetailsInformation? other)
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
               PropertyName == other.PropertyName &&
               detailsClassName == other.detailsClassName &&
               Equals(autoPropertiesInformation, other.autoPropertiesInformation);
    }

    public override bool Equals(object? obj)
    {
        return obj is AutoDetailsInformation other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 17;
            hashCode = (hashCode * 31) + InformationEquality.SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + (PropertyName?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + (detailsClassName?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + (autoPropertiesInformation?.GetHashCode() ?? 0);
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

        if (autoPropertiesInformation is not null)
        {
            AutoDetailsGenerator.Generate(context, detailsClassName!, autoPropertiesInformation!);
        }
    }
}
