namespace RoyalCode.SmartSelector;

/// <summary>
/// Provides the common configuration for attributes that automatically select properties.
/// </summary>
public abstract class AutoPropertiesAttributeBase : Attribute
{
    /// <summary>
    /// An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    /// Can be empty to include all properties.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// An array of property names to generate flattening for complex or nested properties.
    /// Property names are case-sensitive.
    /// </summary>
    public string[]? Flattening { get; set; }
}

/// <summary>
/// <para>
///     This attribute is used with the <see cref="AutoSelectAttribute{TFrom}"/>
///     to indicate that all public properties of the <c>TFrom</c> class should be included in the selection.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoPropertiesAttribute : AutoPropertiesAttributeBase { }

/// <summary>
/// Specifies that a class should automatically implement properties based on the members of the specified type.
/// </summary>
/// <typeparam name="TFrom">
///     The type whose members are used to generate properties for the attributed class.
/// </typeparam>

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoPropertiesAttribute<TFrom> : AutoPropertiesAttributeBase { }
