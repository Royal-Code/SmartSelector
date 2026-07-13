## Release 0.1.0

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
RCSS000  | Usage    | Error    | RCSS000_Invalid_model_type, [Documentation](https://google.com)
RCSS001  | Usage    | Error    | RCSS001_It_is_not_possible_to_determine_a_corresponding_property_for_the_other_type, [Documentation](https://google.com)
RCSS002  | Usage    | Error    | RCSS002_Incompatible_property_types, [Documentation](https://google.com)
RCSS003  | Usage    | Error    | RCSS003_Invalid_auto_property, [Documentation](https://google.com)
RCSS004  | Usage    | Error    | RCSS004_Conflict_auto_property_attribute, [Documentation](https://google.com)
RCSS005  | Usage    | Error    | RCSS005_Invalid_auto_property_from, [Documentation](https://google.com)

## Release 0.5.0

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
