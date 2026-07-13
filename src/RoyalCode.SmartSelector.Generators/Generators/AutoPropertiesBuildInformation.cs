namespace RoyalCode.SmartSelector.Generators.Generators;

/// <summary>
/// Transient semantic model used only inside a Transform. It must never cross the
/// incremental pipeline boundary; <see cref="ToInformation"/> produces the retained snapshot.
/// </summary>
internal sealed class AutoPropertiesBuildInformation(
    TypeDescriptor originType,
    PropertyDescriptor[] properties,
    AutoDetailsInformation[] autoDetails)
{
    internal TypeDescriptor OriginType { get; } = originType;
    internal PropertyDescriptor[] Properties { get; } = properties;
    internal AutoDetailsInformation[] AutoDetails { get; } = autoDetails;

    internal AutoPropertiesInformation ToInformation() => new(
        TypeSnapshot.Create(OriginType),
        Properties.Select(PropertySnapshot.Create).ToArray(),
        AutoDetails);
}
