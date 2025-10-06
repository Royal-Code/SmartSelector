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
    /// Initializes a new instance of the AutoPropertiesAttribute class.
    /// </summary>
    public AutoPropertiesAttribute() { }

    /// <summary>
    /// Initializes a new instance of the AutoPropertiesAttribute class, specifying property names to exclude from
    /// automatic processing.
    /// </summary>
    /// <remarks>Use this constructor to prevent specific properties from being affected by the attribute's
    /// automatic behavior. This is useful when certain properties require custom logic or should not be included in the
    /// automated process.</remarks>
    /// <param name="excludeProperties">An array of property names to exclude from automatic property handling. Property names are case-sensitive. Can
    /// be empty to include all properties.</param>
    public AutoPropertiesAttribute(params string[] excludeProperties) { }
}
