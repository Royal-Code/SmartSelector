
### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
RCSS006  | Usage    | Error    | AutoPropertiesAttribute&lt;TFrom&gt; requires a partial destination class
RCSS007  | Usage    | Error    | Non-generic AutoPropertiesAttribute requires AutoSelectAttribute&lt;TFrom&gt;
RCSS008  | Usage    | Error    | Generic destination DTOs and generic containing types are unsupported
RCSS010  | Usage    | Warning  | A destination property matches multiple flattened source paths
RCSS011  | Usage    | Error    | Destination DTOs in the global namespace are unsupported
RCSS012  | Usage    | Error    | AutoDetails target type already exists and is not a partial class
RCSS013  | Usage    | Error    | Two AutoDetails properties generate the same type
RCSS014  | Usage    | Error    | AutoDetails target type is less accessible than the property
RCSS015  | Usage    | Warning  | Nullable source projected into non-nullable destination
RCSS016  | Usage    | Info     | Nullable source collection projected as an empty collection when null
RCSS017  | Usage    | Error    | MapFrom path does not resolve to a readable public source property
