# SmartSelector 0.5.0

## Breaking changes

- `AutoSelectAttribute<TFrom>`, `AutoPropertiesAttribute`, `AutoPropertiesAttribute<TFrom>` and `AutoDetailsAttribute` are now sealed. Consumer attributes can no longer inherit from them.
- `Exclude` and `Flattening` are now declared by the new public abstract `AutoPropertiesAttributeBase`, shared by both `AutoProperties` forms and `AutoDetails`.
- `AssemblyVersion` follows the package version during the 0.x series and therefore advances from `0.4.0` to `0.5.0` for this breaking release.

## Migration

- Replace custom attributes derived from SmartSelector attributes with direct use of the corresponding built-in attribute. Put reusable conventions in application code or generator configuration instead of attribute inheritance.
- Recompile consumers against 0.5.0. The configuration properties remain source-compatible through inheritance, but their declaring type changed, which is a binary compatibility break and affects reflection code using `DeclaredOnly`.
- `AutoPropertiesAttributeBase` exists to share the public configuration contract. Custom attributes derived only from this base are not recognized automatically by the SmartSelector generator.
