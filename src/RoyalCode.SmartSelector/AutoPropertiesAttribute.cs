namespace RoyalCode.SmartSelector;

/// <summary>
/// <para>
///     This attribute is used with the <see cref="AutoSelectAttribute{TFrom}"/> 
///     to indicate that all public properties of the <c>TFrom</c> class should be included in the selection.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AutoPropertiesAttribute : Attribute 
{
    /// <summary>
    /// An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    /// Can be empty to include all properties.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// Initializes a new instance of the AutoPropertiesAttribute class.
    /// </summary>
    public AutoPropertiesAttribute() { }

    /////// <summary>
    /////// Initializes a new instance of the AutoPropertiesAttribute class, specifying property names to exclude from
    /////// automatic processing.
    /////// </summary>
    /////// <param name="excludeProperties">
    ///////     An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    ///////     Can be empty to include all properties.
    /////// </param>
    ////public AutoPropertiesAttribute(params string[] excludeProperties) { }
}

/// <summary>
/// Specifies that a class should automatically implement properties based on the members of the specified type.
/// </summary>
/// <typeparam name="TFrom">
///     The type whose members are used to generate properties for the attributed class.
/// </typeparam>

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AutoPropertiesAttribute<TFrom> : Attribute
{
    /// <summary>
    /// An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    /// Can be empty to include all properties.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// Initializes a new instance of the AutoPropertiesAttribute class.
    /// </summary>
    public AutoPropertiesAttribute() { }

    /////// <summary>
    /////// Initializes a new instance of the AutoPropertiesAttribute class, specifying property names to exclude from
    /////// automatic processing.
    /////// </summary>
    /////// <param name="excludeProperties">
    ///////     An array of property names to exclude from automatic property handling. Property names are case-sensitive.
    ///////     Can be empty to include all properties.
    /////// </param>
    ////public AutoPropertiesAttribute(params string[] excludeProperties) { }
}