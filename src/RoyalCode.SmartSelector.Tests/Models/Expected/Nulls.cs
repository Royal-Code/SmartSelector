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