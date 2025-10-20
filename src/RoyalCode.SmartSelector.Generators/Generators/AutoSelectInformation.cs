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
        AutoPropertiesInformation? autoPropertyInformation)
    {
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

    public bool Equals(AutoSelectInformation other)
    {
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;

        return diagnostics?.SequenceEqual(other.diagnostics) == true &&
               matchSelection?.Equals(other.matchSelection!) == true;
    }

    public override bool Equals(object? obj)
    {
        return obj is AutoSelectInformation other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = 2147442367;
        hashCode = hashCode * -1521134295 + EqualityComparer<Diagnostic[]?>.Default.GetHashCode(diagnostics);
        hashCode = hashCode * -1521134295 + EqualityComparer<MatchSelection?>.Default.GetHashCode(matchSelection);
        return hashCode;
    }
}
