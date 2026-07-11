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
        return SequenceEqual(diagnostics, other.diagnostics) &&
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
            hashCode = (hashCode * 31) + SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + (detailsClassName?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + (autoPropertiesInformation?.GetHashCode() ?? 0);
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

        if (autoPropertiesInformation is not null)
        {
            AutoDetailsGenerator.Generate(context, detailsClassName!, autoPropertiesInformation!);
        }
    }
}
