namespace RoyalCode.SmartSelector;

/// <summary>
/// <para>
///     This attribute should be used in properties of DTO/Details class types that have 
///     the <see cref="AutoPropertiesAttribute{TFrom}"/> or
///     <see cref="AutoSelectAttribute{TFrom}"/> with <see cref="AutoPropertiesAttribute"/>.
/// </para>
/// <para>
///     The class of the property type will be generated.
///     <br />
///     This generation will be similar to AutoProperties, using the property related to TFrom as the source type.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class AutoDetailsAttribute : Attribute 
{
    /// <summary>
    /// An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    /// Can be empty to include all properties.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// An array of property names to generate flattening for complex/nested properties.
    /// Property names are case-sensitive.
    /// </summary>
    public string[]? Flattening { get; set; }
}
