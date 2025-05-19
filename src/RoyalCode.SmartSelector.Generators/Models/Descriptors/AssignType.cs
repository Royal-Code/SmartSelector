namespace RoyalCode.SmartSelector.Generators.Models.Descriptors;

public enum AssignType
{
    /// <summary>
    /// Direct assignment, no conversion needed.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    ///     Name = e.Name,
    ///     UserName = e.User.Name,
    /// </code>
    /// </remarks>
    Direct,

    /// <summary>
    /// Simple cast, no conversion needed.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    ///     Status = (Status)e.Status,
    ///     UserStatus = (Status)e.User.Status,
    /// </code>
    /// </remarks>
    SimpleCast,

    /// <summary>
    /// Represents a nullable assignment performed using a ternary operator.
    /// This allows for conditional assignment based on the presence of a value.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    ///     LastLogin = e.LastLogin.HasValue ? e.LastLogin.Value : default(DateTime),
    /// </code>
    /// </remarks>
    NullableTernary,

    /// <summary>
    /// Represents a nullable assignment performed using a ternary operator with a cast.
    /// This allows for conditional assignment based on the presence of a value.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    ///     LastLogin = e.LastLogin.HasValue ? (DateTime)e.LastLogin.Value : default(DateTime),
    /// </code>
    /// </remarks>
    NullableTernaryCast,

    /// <summary>
    /// It is required to create a new object for the target property type,
    /// mapping the properties between the two objects.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    ///     User = new UserDto
    ///     {
    ///         Name = e.Name,
    ///         Status = (Status)e.Status
    ///     },
    /// </code>
    /// </remarks>
    NewInsatance,
}
