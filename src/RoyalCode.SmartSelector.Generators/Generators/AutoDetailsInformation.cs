using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoDetailsInformation : IEquatable<AutoDetailsInformation>
{
    private readonly Diagnostic[]? diagnostics;

    private readonly string? detailsClassName;
    private readonly AutoPropertiesInformation? autoPropertiesInformation;

    public AutoDetailsInformation(Diagnostic diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoDetailsInformation(
        string detailsClassName,
        AutoPropertiesInformation autoPropertiesInformation)
    {
        this.detailsClassName = detailsClassName;
        this.autoPropertiesInformation = autoPropertiesInformation;
    }

    public bool Equals(AutoDetailsInformation other)
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
               detailsClassName == other.detailsClassName &&
               Equals(autoPropertiesInformation, other.autoPropertiesInformation);
    }

    public override bool Equals(object obj)
    {
        return obj is AutoDetailsInformation other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = 2147442367;
        hashCode = (hashCode * -1521134295) + (diagnostics != null ? diagnostics.GetHashCode() : 0);
        hashCode = (hashCode * -1521134295) + (detailsClassName != null ? detailsClassName.GetHashCode() : 0);
        hashCode = (hashCode * -1521134295) + (autoPropertiesInformation != null ? autoPropertiesInformation.GetHashCode() : 0);
        return hashCode;
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

        if (autoPropertiesInformation is not null)
        {
            AutoDetailsGenerator.Generate(context, detailsClassName!, autoPropertiesInformation!);
        }
    }
}
