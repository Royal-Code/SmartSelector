using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoyalCode.SmartSelector.Generators.Models.Descriptors;

namespace RoyalCode.SmartSelector.Generators.Models;

internal class MatchSelection
{
    private readonly TypeDescriptor originType;
    private readonly IReadOnlyList<PropertyDescriptor> originProperties;
    private readonly TargetTypeInfo targetType;

    public MatchSelection(
        TypeDescriptor originType,
        IReadOnlyList<PropertyDescriptor> originProperties,
        TargetTypeInfo targetType)
    {
        this.originType = originType;
        this.originProperties = originProperties;
        this.targetType = targetType;
    }
}

public readonly struct TargetTypeInfo
{
    public TargetTypeInfo(ITypeSymbol typeSymbol, TypeSyntax typeSyntax, IReadOnlyList<PropertyDescriptor> properties)
    {
        TypeSymbol = typeSymbol;
        TypeSyntax = typeSyntax;
        Properties = properties;
    }

    public ITypeSymbol TypeSymbol { get; }

    public TypeSyntax TypeSyntax { get; }

    public IReadOnlyList<PropertyDescriptor> Properties { get; }
}

