using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoSelectInformation : IEquatable<AutoSelectInformation>
{
    private readonly DiagnosticInfo[]? diagnostics;
    private readonly MatchSelectionSnapshot? matchSelection;
    private readonly AutoPropertiesInformation? autoPropertyInformation;
    private readonly bool hideExpressionMember;
    private readonly bool hideFromMember;

    public AutoSelectInformation(DiagnosticInfo diagnostic)
    {
        diagnostics = [diagnostic];
    }

    public AutoSelectInformation(DiagnosticInfo[] diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public AutoSelectInformation(
        MatchSelectionSnapshot matchSelection,
        AutoPropertiesInformation? autoPropertyInformation,
        DiagnosticInfo[]? diagnostics = null,
        bool hideExpressionMember = false,
        bool hideFromMember = false)
    {
        this.diagnostics = diagnostics;
        this.autoPropertyInformation = autoPropertyInformation;
        this.matchSelection = matchSelection;
        this.hideExpressionMember = hideExpressionMember;
        this.hideFromMember = hideFromMember;
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

        if (autoPropertyInformation is not null)
        {
            autoPropertyInformation.Generate(context);
        }

        if (matchSelection is not null)
        {
            AutoSelectGenerator.Generate(matchSelection, context, hideExpressionMember, hideFromMember);
        }
    }

    public bool Equals(AutoSelectInformation? other)
    {
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;

        return InformationEquality.SequenceEqual(diagnostics, other.diagnostics) &&
               Equals(matchSelection, other.matchSelection) &&
               Equals(autoPropertyInformation, other.autoPropertyInformation) &&
               hideExpressionMember == other.hideExpressionMember &&
               hideFromMember == other.hideFromMember;
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
            hashCode = (hashCode * 31) + InformationEquality.SequenceHashCode(diagnostics);
            hashCode = (hashCode * 31) + (matchSelection?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + (autoPropertyInformation?.GetHashCode() ?? 0);
            hashCode = (hashCode * 31) + hideExpressionMember.GetHashCode();
            hashCode = (hashCode * 31) + hideFromMember.GetHashCode();
            return hashCode;
        }
    }

}
