using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoSelectInformation : IEquatable<AutoSelectInformation>
{
    private readonly Diagnostic[]? diagnostics;
    private readonly MatchSelection? matchSelection;
    private readonly AutoPropertiesInformation? autoPropertyInformation;

    public AutoSelectInformation(Diagnostic diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoSelectInformation(Diagnostic[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoSelectInformation(
        MatchSelection matchSelection,
        AutoPropertiesInformation? autoPropertyInformation,
        Diagnostic[]? diagnostics = null)
    {
        this.diagnostics = diagnostics;
        this.autoPropertyInformation = autoPropertyInformation;
        this.matchSelection = matchSelection;
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

        if (autoPropertyInformation is not null)
        {
            autoPropertyInformation.Generate(context);
        }

        if (matchSelection is not null)
        {
            AutoSelectGenerator.Generate(matchSelection, context);
        }
    }

    public bool Equals(AutoSelectInformation? other)
    {
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;

        return SequenceEqual(diagnostics, other.diagnostics) &&
               Equals(matchSelection, other.matchSelection) &&
               Equals(autoPropertyInformation, other.autoPropertyInformation);
    }

    public override bool Equals(object? obj)
    {
        return obj is AutoSelectInformation other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 17;
            hashCode = (hashCode * 31) + SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + (matchSelection?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + (autoPropertyInformation?.GetHashCode() ?? 0);
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
}
