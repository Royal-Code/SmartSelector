using Microsoft.CodeAnalysis;
using RoyalCode.SmartSelector.Generators.Extensions;

namespace RoyalCode.SmartSelector.Generators.Models.Descriptors;

internal class EnumerableAssignDescriptorResolver : IAssignDescriptorResolver
{
    public bool TryCreateAssignDescriptor(
        TypeDescriptor leftType,
        TypeDescriptor rightType,
        SemanticModel model,
        out AssignDescriptor? descriptor)
    {
        if (leftType.Symbol is null || rightType.Symbol is null ||
            leftType.Symbol.TryGetEnumerableGenericType(out var leftGenericSymbol) is false ||
            rightType.Symbol.TryGetEnumerableGenericType(out var rightGenericSymbol) is false)
        {
            descriptor = null;
            return false;
        }

        TypeDescriptor leftGenericType = TypeDescriptor.Create(leftGenericSymbol!);
        TypeDescriptor rightGenericType = TypeDescriptor.Create(rightGenericSymbol!);

        AssignDescriptor? genericAssignment = AssignDescriptorFactory.Create(
            leftGenericType, rightGenericType, model);

        if (genericAssignment is not null)
            genericAssignment.RequireSelect = true;

        descriptor = genericAssignment;
        return descriptor is not null;
    }
}
