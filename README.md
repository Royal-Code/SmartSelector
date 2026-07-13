# SmartSelector

SmartSelector is a Roslyn Source Generator for strongly typed DTO projections. It generates reusable `Expression<Func<TSource, TDto>>` selectors, conversion helpers, LINQ extension methods, and optional DTO properties, reducing mapping boilerplate while keeping projections suitable for Entity Framework Core.

## Features

- `[AutoSelect<TSource>]` generates a selector expression, a cached `From` converter, and `Select{Dto}` / `To{Dto}` extensions.
- `[AutoProperties]` and `[AutoProperties<TSource>]` generate supported DTO properties.
- `Exclude` and `Flattening` can be configured on `AutoProperties`, `AutoDetails`, or directly on `AutoSelect`.
- Convention-based flattening maps names such as `CustomerAddressCity` to `Customer.Address.City`.
- `[MapFrom]` maps aliases and explicit nested paths such as `"Warehouse.Location"`.
- Nested objects, collections, and arrays are projected recursively; object arrays use `Select(...).ToArray()`.
- `[AutoDetails]` generates or completes the exact nested DTO type declared by a property.
- Nullable-aware generation propagates null, produces empty non-nullable collections when appropriate, and reports unsafe contracts.
- Nested destination DTOs are supported when the complete declaration chain is non-generic and `partial`.
- Compile-time diagnostics cover invalid usage, incompatible mappings, ambiguous flattening, nullability, and invalid paths.
- The generator package includes analyzer variants for supported Roslyn versions.

SmartSelector focuses on declarative 1:1 mappings. It intentionally does not provide custom resolvers, formatters, callbacks, or global naming policies. Use a manually written LINQ expression for calculations or domain-specific transformations.

## Supported platforms

| Component | Target |
|---|---|
| `RoyalCode.SmartSelector` | .NET 8, .NET 9, .NET 10 |
| `RoyalCode.SmartSelector.Generators` | .NET Standard 2.0 analyzer |

## Installation

Reference both packages at the same version:

```xml
<ItemGroup>
  <PackageReference Include="RoyalCode.SmartSelector" Version="0.5.0" />
  <PackageReference Include="RoyalCode.SmartSelector.Generators"
                    Version="0.5.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

Then import:

```csharp
using RoyalCode.SmartSelector;
```

No runtime registration or dependency injection configuration is required.

## Quick start

Given an entity:

```csharp
public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
```

Declare a partial DTO:

```csharp
using RoyalCode.SmartSelector;

namespace MyApp.Users;

[AutoSelect<User>,
 AutoProperties(Exclude = [nameof(User.PasswordHash)])]
public partial class UserDetails { }
```

Use the generated API:

```csharp
// IQueryable: keeps the projection in the expression tree.
var users = await db.Users
    .SelectUserDetails()
    .ToListAsync(ct);

// Already materialized objects: uses a cached compiled delegate.
UserDetails details = UserDetails.From(user);
UserDetails details2 = user.ToUserDetails();

// Reuse or compose the generated expression directly.
Expression<Func<User, UserDetails>> selector =
    UserDetails.SelectUserExpression;
```

For `User` → `UserDetails`, the generated public contract is:

```csharp
UserDetails.SelectUserExpression;
UserDetails.From(User user);
query.SelectUserDetails();   // IQueryable<User>
items.SelectUserDetails();   // IEnumerable<User>
user.ToUserDetails();        // User
```

## Choosing an attribute

| Requirement | Declaration |
|---|---|
| Selector for manually declared properties | `[AutoSelect<TEntity>]` |
| Selector and supported automatic properties | `[AutoSelect<TEntity>, AutoProperties]` |
| Properties only, without selector or extensions | `[AutoProperties<TEntity>]` |
| Exclude automatic properties | `Exclude = [nameof(TEntity.Property)]` |
| Generate flattened properties for a root navigation | `Flattening = [nameof(TEntity.Navigation)]` |
| Rename or explicitly select a source path | `[MapFrom(...)]` |
| Generate or complete a nested details type | `[AutoDetails]` |

`AutoSelect<TSource>` alone does not generate DTO properties. Declare them manually or add `AutoProperties`. Providing `Exclude` or `Flattening` directly to `AutoSelect` also enables automatic property generation.

Do not combine `AutoSelect<Product>` with `AutoProperties<Product>`. When `AutoSelect<TSource>` is present, use the non-generic `[AutoProperties]` form because the source type is already known.

## Automatic properties

```csharp
[AutoSelect<Product>, AutoProperties]
public partial class ProductDetails { }

[AutoProperties<Product>]
public partial class ProductSnapshot { } // properties only
```

Automatic property generation supports:

- numeric primitives, `bool`, `char`, `string`, `decimal`, and `DateTime`;
- enums and structs, including application value objects;
- supported nullable value/reference types;
- arrays of simple types, enums, or structs;
- generic collections implementing `IEnumerable<T>` when `T` is a supported simple type, enum, or struct.

Complex classes are not added as ordinary automatic properties. Declare a nested DTO, use `AutoDetails`, or flatten the navigation. Properties already declared by the user are not generated again.

## Exclusion and configured flattening

Configure the options on `AutoProperties`:

```csharp
[AutoSelect<Order>,
 AutoProperties(
     Exclude = [nameof(Order.InternalCode)],
     Flattening = [nameof(Order.Customer)])]
public partial class OrderDetails { }
```

Or directly on `AutoSelect`:

```csharp
[AutoSelect<Order>(
    Exclude = [nameof(Order.InternalCode)],
    Flattening = [nameof(Order.Customer)])]
public partial class OrderDetails { }
```

Both options are case-sensitive. If `Customer` has supported `Name` and `Email` properties, configured flattening generates `CustomerName` and `CustomerEmail` and omits the complex `Customer` property from automatic generation.

## Convention-based flattening

A manually declared DTO property can represent a deep path by concatenating source property names:

```csharp
[AutoSelect<Order>]
public partial class OrderDetails
{
    public string CustomerAddressCountryRegionName { get; set; } = string.Empty;
}
```

The generated assignment is equivalent to:

```csharp
CustomerAddressCountryRegionName =
    source.Customer.Address.Country.Region.Name;
```

If a destination name matches multiple source paths, SmartSelector reports `RCSS010`. Rename it or use `MapFrom`.

## Explicit mapping with `MapFrom`

Use `nameof` for direct members and a dot-separated string for nested paths:

```csharp
[AutoSelect<Supplier>]
public partial class SupplierDetails
{
    [MapFrom(nameof(Supplier.Name))]
    public string DisplayName { get; set; } = string.Empty;

    [MapFrom("Warehouse.Location")]
    public string? Location { get; set; }
}
```

Every segment must be a readable public property. An explicit nested path takes precedence over similarly named direct properties. Invalid nested paths report `RCSS017` on the destination property.

## Nested objects, collections, and arrays

Declare the desired shape and SmartSelector projects it recursively:

```csharp
[AutoSelect<Post>]
public partial class PostDetails
{
    public string Title { get; set; } = string.Empty;
    public AuthorDetails Author { get; set; } = new();
    public IReadOnlyList<CommentDetails> Comments { get; set; } = [];
}

public class AuthorDetails
{
    public string Name { get; set; } = string.Empty;
}

public class CommentDetails
{
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}
```

Object collections are emitted with `Select(...).ToList()` when the destination requires a list. A destination such as `CommentDetails[]` is emitted with `Select(...).ToArray()`.

## Generated nested types with `AutoDetails`

`AutoDetails` generates or completes the exact type declared by the property:

```csharp
[AutoSelect<Customer>, AutoProperties]
public partial class CustomerDetails
{
    [AutoDetails(Exclude = [nameof(Address.InternalCode)])]
    public AddressDto Address { get; set; } = new();
}
```

If `AddressDto` does not exist, it is generated from the matching source property type. If it exists, it must be `partial`; existing properties are preserved and only missing supported properties are generated. `AutoDetails` also accepts `Flattening`.

Only one property may request generation of a given details type, and an existing type must be accessible enough for the property.

## Nullable-aware projections

With nullable reference types enabled, SmartSelector applies directional null handling:

| Source | Destination | Behavior |
|---|---|---|
| nullable scalar/navigation | nullable destination | propagates `null`, adding a conditional when required |
| nullable collection | nullable collection | propagates `null` |
| nullable collection | non-nullable collection | produces an empty collection and reports `RCSS016` (Info) |
| nullable array | non-nullable array | uses `Array.Empty<T>()` and reports `RCSS016` (Info) |
| nullable scalar/navigation | non-nullable destination | preserves previous behavior and reports `RCSS015` (Warning) |

Treat `RCSS015` as a DTO contract issue. Make the destination nullable, change the source model, or exclude the property. SmartSelector does not invent scalar or object defaults to conceal a mismatch.

Nullable-oblivious code (`#nullable disable`) retains its previous behavior without annotation-based guards or diagnostics.

## Nested destination DTOs

Nested DTOs are supported when every declaration is non-generic and `partial`:

```csharp
public partial class Contracts
{
    [AutoSelect<User>]
    public partial class UserDetails
    {
        public int Id { get; set; }
    }
}
```

Destination DTOs must be declared in a namespace. Generic destination DTOs and DTOs inside generic containing types report `RCSS008`.

The `TSource` argument may be namespace-qualified, use `global::`, refer to a nested type, or be a constructed generic type such as `Envelope<string>`.

## Entity Framework Core

The `IQueryable<TSource>` extension applies the generated expression directly:

```csharp
var page = await db.Orders
    .Where(order => order.Active)
    .OrderBy(order => order.Id)
    .SelectOrderDetails()
    .Take(50)
    .ToListAsync(ct);

var query = db.Orders.Select(OrderDetails.SelectOrderExpression);
```

Apply entity filters and ordering before projection when they depend on members absent from the DTO. Translation depends on the EF Core provider and version, so integration-test important projections with the production provider.

Use `From`, `To{Dto}`, and the `IEnumerable<TSource>` extension for already materialized objects; do not invoke `From` inside an `IQueryable` expression.

## Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `RCSS000` | Error | invalid `AutoSelect` usage, including a non-partial declaration chain |
| `RCSS001` | Error | no corresponding source property was found |
| `RCSS002` | Error | source and destination property types are incompatible |
| `RCSS003` | Error | generic `AutoProperties<TSource>` used with `AutoSelect` |
| `RCSS004` | Error | generic and non-generic `AutoProperties` used together |
| `RCSS005` | Error | invalid `AutoProperties<TSource>` type argument |
| `RCSS006` | Error | `AutoProperties<TSource>` destination is not partial |
| `RCSS007` | Error | non-generic `AutoProperties` has no `AutoSelect<TSource>` |
| `RCSS008` | Error | generic destination DTO or generic containing type |
| `RCSS010` | Warning | ambiguous convention-based flattened path |
| `RCSS011` | Error | destination DTO is in the global namespace |
| `RCSS012` | Error | existing `AutoDetails` target type is not partial |
| `RCSS013` | Error | multiple properties request the same `AutoDetails` type |
| `RCSS014` | Error | `AutoDetails` target type has insufficient accessibility |
| `RCSS015` | Warning | nullable source flows into a non-nullable destination |
| `RCSS016` | Info | nullable collection is projected as empty when null |
| `RCSS017` | Error | nested `MapFrom` path is invalid or unreadable |

There is no `RCSS009` rule in version 0.5.0.

## Generator compatibility

The generator package contains analyzer variants selected by the compiler API version:

| Validated SDK | SDK Roslyn | Loaded variant | Minimum requirement |
|---|---:|---|---|
| 8.0.422 | 4.8 | `roslyn4.8` | Roslyn 4.8 / .NET SDK 8.0.4xx |
| 9.0.100 | 4.12 | `roslyn4.8` | Roslyn 4.8 / .NET SDK 8.0.4xx |
| 10.0.301 | 5.6 | `roslyn5.6` | Roslyn 5.6 / .NET SDK 10.0.3xx |

`RoyalCode.Extensions.SourceGenerator` is shipped beside each analyzer variant. This matrix was validated by building and executing a consumer application for each SDK family.

## Limitations

- No custom resolvers, formatters, conditional mapping callbacks, or global naming policies.
- No generic destination DTOs or generic destination containing types.
- No destination DTOs in the global namespace.
- `MapFrom` supports readable public property paths, not methods, fields, or indexers.
- Provider-specific LINQ translation must be validated by the consuming application.

## Documentation and samples

- [Detailed usage guide](src/docs.md)
- [Selector reference](src/.docs/selector.md)
- [Rules for AI tools and coding agents](src/.docs/selector.ai-rules.md)
- [Demo project](src/RoyalCode.SmartSelector.Demo)
- [Benchmarks](src/RoyalCode.SmartSelector.Benchmarks)
- [Release notes](src/RELEASE_NOTES.md)

## License

SmartSelector is licensed under the [GNU Affero General Public License v3.0](LICENSE).
