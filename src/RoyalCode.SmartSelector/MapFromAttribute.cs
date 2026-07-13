namespace RoyalCode.SmartSelector;

/// <summary>
/// Indicates that the decorated property should map its value from another property with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)] 
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the MapFromAttribute class, specifying the source property name to map from.
    /// </summary>
    /// <param name="propertyName">The source property name or dot-separated property path. Cannot be null or empty.</param>
    public MapFromAttribute(string propertyName)
    { 
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets or sets the source property name or dot-separated property path to map from.
    /// </summary>
    public string PropertyName { get; set; }
}
