using RoyalCode.SmartSelector.Extensions;
using RoyalCode.SmartSelector.Generators.Models.Descriptors;

namespace RoyalCode.SmartSelector.Generators.Models;

internal class PropertyMatch
{
}

internal class PropertySelection
{
    private readonly PropertyDescriptor property;

    public PropertySelection(PropertyDescriptor property)
    {
        this.property = property;
    }

    /// <summary>
    /// The current selected property type.
    /// </summary>
    public PropertyDescriptor PropertyType => property;

    /// <summary>
    /// The declaring class type of the root selected property.
    /// if this selection does not have a parent, this selection will be the root.
    /// </summary>
    public PropertyDescriptor RootDeclaringType => Parent != null ? Parent.RootDeclaringType : property;

    /// <summary>
    /// The parent <see cref="PropertySelection"/>. Can be null.
    /// </summary>
    public PropertySelection? Parent { get; private set; }

    public static PropertySelection? Select(PropertyDescriptor property, TargetTypeInfo targetType)
    {
        PropertySelection? ps =null;

        var targetProperty = targetType.Properties.FirstOrDefault(p => p.Name == property.Name);
        
        if(targetProperty != null)
        {
            return new PropertySelection(targetProperty);
        }

        var parts = property.Name.SplitUpperCase();
        if (parts is not null)
        {
            var partSelector = new PropertySelectionPart(parts, targetType);
            ps = partSelector.Select();
        }

        return ps;
    }
}

internal class PropertySelectionPart
{
    private readonly string[] parts;
    private readonly TargetTypeInfo targetType;
    private readonly int position;
    private readonly PropertySelection? parent;

    public PropertySelectionPart(
        string[] parts, 
        TargetTypeInfo targetType,
        int position = 0,
        PropertySelection? parent = null)
    {
        this.parts = parts;
        this.targetType = targetType;
        this.position = position;
        this.parent = parent;
    }

    internal PropertySelection? Select()
    {
        var currentProperty = string.Empty;

        for (int i = position; i < parts.Length; i++)
        {
            currentProperty += parts[i];
            var property = targetType.Properties.FirstOrDefault(p => p.Name == currentProperty);
            if (property is not null)
            {
                var ps = parent == null ? new PropertySelection(property) : parent.SelectChild(property);
                if (i + 1 < parts.Length)
                {
                    var nextPart = new PropertySelectionPart(parts, property.PropertyType, i + 1, ps);
                    var nextPs = nextPart.Select();
                    if (nextPs is not null)
                        return nextPs;
                }
                else
                {
                    return ps;
                }
            }
        }

        return null;
    }
}