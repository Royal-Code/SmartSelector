using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal class AutoSelectInformation
{
    private Diagnostic diagnostic;

    public AutoSelectInformation(Diagnostic diagnostic)
    {
        this.diagnostic = diagnostic;
    }

    internal void Generate(SourceProductionContext context)
    {
        throw new NotImplementedException();
    }
}
