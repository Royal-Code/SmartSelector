using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

public class StringNulls
{
    public string Value1 { get; set; } = default!;
    public string Value2 { get; set; } = default!;
    public string? Value3 { get; set; }
    public string? Value4 { get; set; }
}

[AutoSelect<StringNulls>]
public partial class StringNullsDto
{
    public string Value1 { get; set; } = default!;
    public string? Value2 { get; set; }
    public string Value3 { get; set; } = default!;
    public string? Value4 { get; set; }
}

// expected
public partial class StringNullsDto
{
    private static Func<StringNulls, StringNullsDto> selectStringNullsFunc;

    public static Expression<Func<StringNulls, StringNullsDto>> SelectStringNullsExpression { get; } = a => new StringNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3,
        Value4 = a.Value4
    };

    public static StringNullsDto From(StringNulls stringNulls) => (selectStringNullsFunc ??= SelectStringNullsExpression.Compile())(stringNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


public class IntNulls
{
    public int Value1 { get; set; } = default!;
    public int Value2 { get; set; } = default!;
    public int? Value3 { get; set; }
    public int? Value4 { get; set; }
}

[AutoSelect<IntNulls>]
public partial class IntNullsDto
{
    public int Value1 { get; set; } = default!;
    public int? Value2 { get; set; }
    public int Value3 { get; set; } = default!;
    public int? Value4 { get; set; }
}

// expected
public partial class IntNullsDto
{
    private static Func<IntNulls, IntNullsDto> selectIntNullsFunc;

    public static Expression<Func<IntNulls, IntNullsDto>> SelectIntNullsExpression { get; } = a => new IntNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3.HasValue ? a.Value3.Value : default,
        Value4 = a.Value4
    };

    public static IntNullsDto From(IntNulls intNulls) => (selectIntNullsFunc ??= SelectIntNullsExpression.Compile())(intNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


public enum NullsKind
{
    None,
    Nullable,
    NonNullable
}

public class EnumNulls
{
    public NullsKind Value1 { get; set; } = default!;
    public NullsKind Value2 { get; set; } = default!;
    public NullsKind? Value3 { get; set; }
    public NullsKind? Value4 { get; set; }
}

[AutoSelect<EnumNulls>]
public partial class EnumNullsDto
{
    public NullsKind Value1 { get; set; } = default!;
    public NullsKind? Value2 { get; set; }
    public NullsKind Value3 { get; set; } = default!;
    public NullsKind? Value4 { get; set; }
}

// expected
public partial class EnumNullsDto
{
    private static Func<EnumNulls, EnumNullsDto> selectEnumNullsFunc;

    public static Expression<Func<EnumNulls, EnumNullsDto>> SelectEnumNullsExpression { get; } = a => new EnumNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3.HasValue ? a.Value3.Value : default,
        Value4 = a.Value4
    };

    public static EnumNullsDto From(EnumNulls enumNulls) => (selectEnumNullsFunc ??= SelectEnumNullsExpression.Compile())(enumNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


public enum NullsType
{
    None,
    Nullable,
    NonNullable
}

[AutoSelect<EnumNulls>]
public partial class EnumNullsTypeDto
{
    public NullsType Value1 { get; set; } = default!;
    public NullsType? Value2 { get; set; }
    public NullsType Value3 { get; set; } = default!;
    public NullsType? Value4 { get; set; }
}

// expected
public partial class EnumNullsTypeDto
{
    private static Func<EnumNulls, EnumNullsTypeDto> selectEnumNullsTypeFunc;

    public static Expression<Func<EnumNulls, EnumNullsTypeDto>> SelectEnumNullsTypeExpression { get; } = a => new EnumNullsTypeDto
    {
        Value1 = (NullsType)a.Value1,
        Value2 = (NullsType?)a.Value2,
        Value3 = a.Value3.HasValue ? (NullsType)a.Value3.Value : default,
        Value4 = (NullsType?)a.Value4
    };

    public static EnumNullsTypeDto From(EnumNulls enumNulls) => (selectEnumNullsTypeFunc ??= SelectEnumNullsTypeExpression.Compile())(enumNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


public class EnumerableNulls
{
    public IEnumerable<string> Value1 { get; set; } = default!;
    public IEnumerable<string> Value2 { get; set; } = default!;
    public IEnumerable<string>? Value3 { get; set; }
    public IEnumerable<string>? Value4 { get; set; }
}

[AutoSelect<EnumerableNulls>]
public partial class EnumerableNullsDto
{
    public IEnumerable<string> Value1 { get; set; } = default!;
    public IEnumerable<string>? Value2 { get; set; }
    public IEnumerable<string> Value3 { get; set; } = default!;
    public IEnumerable<string>? Value4 { get; set; }
}

// expected
public partial class EnumerableNullsDto
{
    private static Func<EnumerableNulls, EnumerableNullsDto> selectEnumerableNullsFunc;

    public static Expression<Func<EnumerableNulls, EnumerableNullsDto>> SelectEnumerableNullsExpression { get; } = a => new EnumerableNullsDto
    {
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3,
        Value4 = a.Value4
    };

    public static EnumerableNullsDto From(EnumerableNulls enumerableNulls) => (selectEnumerableNullsFunc ??= SelectEnumerableNullsExpression.Compile())(enumerableNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


[AutoSelect<EnumerableNulls>]
public partial class CollectionsNullsDto
{
    public ICollection<string> Value1 { get; set; } = default!;
    public ICollection<string>? Value2 { get; set; }
    public ICollection<string> Value3 { get; set; } = default!;
    public ICollection<string>? Value4 { get; set; }
}

// expected
public partial class CollectionsNullsDto
{
    private static Func<EnumerableNulls, CollectionsNullsDto> selectEnumerableNullsFunc;

    public static Expression<Func<EnumerableNulls, CollectionsNullsDto>> SelectEnumerableNullsExpression { get; } = a => new CollectionsNullsDto
    {
        Value1 = a.Value1.ToList(),
        Value2 = a.Value2.ToList(),
        Value3 = a.Value3.ToList(),
        Value4 = a.Value4.ToList()
    };

    public static CollectionsNullsDto From(EnumerableNulls enumerableNulls) => (selectEnumerableNullsFunc ??= SelectEnumerableNullsExpression.Compile())(enumerableNulls);
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


public class ValueObject
{
    public string Value { get; set; } = default!;
}

public class ValueSelectNulls
{
    public ICollection<ValueObject> Value1 { get; set; } = default!;
    public ICollection<ValueObject> Value2 { get; set; } = default!;
    public ICollection<ValueObject>? Value3 { get; set; }
    public ICollection<ValueObject>? Value4 { get; set; }
}

public class ValueDto
{
    public string Value { get; set; } = default!;
}

[AutoSelect<ValueSelectNulls>]
public partial class ValueSelectNullsDto
{
    public IReadOnlyList<ValueDto> Value1 { get; set; } = default!;
    public IReadOnlyList<ValueDto>? Value2 { get; set; }
    public IReadOnlyList<ValueDto> Value3 { get; set; } = default!;
    public IReadOnlyList<ValueDto>? Value4 { get; set; }
}

// expected
public partial class ValueSelectNullsDto
{
    private static Func<ValueSelectNulls, ValueSelectNullsDto> selectValueSelectNullsFunc;

    public static Expression<Func<ValueSelectNulls, ValueSelectNullsDto>> SelectValueSelectNullsExpression { get; } = a => new ValueSelectNullsDto
    {
        Value1 = a.Value1.Select(b => new ValueDto 
        { 
            Value = b.Value 
        }).ToList(),
        Value2 = a.Value2.Select(b => new ValueDto 
        { 
            Value = b.Value 
        }).ToList(),
        Value3 = a.Value3.Select(b => new ValueDto 
        { 
            Value = b.Value 
        }).ToList(),
        Value4 = a.Value4.Select(b => new ValueDto 
        { 
            Value = b.Value 
        }).ToList()
    };
    
    public static ValueSelectNullsDto From(ValueSelectNulls valueSelectNulls) => (selectValueSelectNullsFunc ??= SelectValueSelectNullsExpression.Compile())(valueSelectNulls);
}
