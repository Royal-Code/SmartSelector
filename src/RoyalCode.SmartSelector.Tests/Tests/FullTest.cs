using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class FullTest
{
    [Fact]
    public void SelectWithAllPossibilities()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);
    }

    //[Fact]
    public void Select_Foo_Bar_Baz_x4_Nullable_Possibilities()
    {
        Util.Compile(FooCode.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(FooCode.ExpectedPartial);
    }
}

file static class Code
{
    public const string Types =
"""
using RoyalCode.SmartSelector;
using System.Collections.Generic;

namespace Tests.SmartSelector.Models;

#nullable disable // poco

public class EntityWithSubSelectsAndCollections
{
    public int Id { get; set; }

    public EntitySubTypeWithCollection SubValue { get; set; } = null!;

    public ICollection<EntitySubType> SubTypes { get; set; } = null!;
}

public class EntitySubType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    // a nullable entity property and a non-nullable dto property
    public int? Money { get; set; }

    public string OtherProperty { get; set; } = null!;
}

public class EntitySubTypeWithCollection
{
    public int Id { get; set; }

    public ICollection<EntityItemWithSubType> CollectionOfSubItems { get; set; } = null!;
}

public class EntityItemWithSubType
{
    public int Id { get; set; }

    public EntityWithCollectionOfItem PropertyWithItem { get; set; } = null!;

    public EntityWithCollectionOfInt PropertyWithInt { get; set; } = null!;
}

public class EntityWithCollectionOfItem
{
    public int Id { get; set; }

    public ICollection<EntityItem> Values { get; set; } = null!;
}

public class EntityItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
}

public class EntityWithCollectionOfInt
{
    public int Id { get; set; }

    public ICollection<int> Values { get; set; } = null!;
}

[AutoSelect<EntityWithSubSelectsAndCollections>]
public partial class DtoWithSubSelectsAndCollections
{
    public int Id { get; set; }

    public DtoSubTypeWithCollection SubValue { get; set; } = null!;

    public IEnumerable<DtoSubType> SubTypes { get; set; } = null!;
}

public class DtoSubType
{
    public int Id { get; set; }

    // a dto property with ? and a non-nullable entity property
    public string? Name { get; set; }

    public int Money { get; set; }
}

public class DtoSubTypeWithCollection
{
    public int Id { get; set; }

    public IEnumerable<DtoItemWithSubType> CollectionOfSubItems { get; set; } = null!;
}

public class DtoItemWithSubType
{
    public int Id { get; set; }

    public DtoWithCollectionOfItem PropertyWithItem { get; set; } = null!;

    public DtoWithCollectionOfInt PropertyWithInt { get; set; } = null!;
}

public class DtoWithCollectionOfInt
{
    public IEnumerable<int> Values { get; set; } = null!;
}

public class DtoWithCollectionOfItem
{
    public int Id { get; set; }

    public IEnumerable<DtoItem> Values { get; set; } = null!;
}

public class DtoItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
}

""";
    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class DtoWithSubSelectsAndCollections
{
    private static Func<EntityWithSubSelectsAndCollections, DtoWithSubSelectsAndCollections> selectEntityWithSubSelectsAndCollectionsFunc;

    public static Expression<Func<EntityWithSubSelectsAndCollections, DtoWithSubSelectsAndCollections>> SelectEntityWithSubSelectsAndCollectionsExpression { get; } = a => new DtoWithSubSelectsAndCollections
    {
        Id = a.Id,
        SubValue = new DtoSubTypeWithCollection
        {
            Id = a.SubValue.Id,
            CollectionOfSubItems = a.SubValue.CollectionOfSubItems.Select(b => new DtoItemWithSubType
            {
                Id = b.Id,
                PropertyWithItem = new DtoWithCollectionOfItem
                {
                    Id = b.PropertyWithItem.Id,
                    Values = b.PropertyWithItem.Values.Select(c => new DtoItem
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                },
                PropertyWithInt = new DtoWithCollectionOfInt
                {
                    Values = b.PropertyWithInt.Values
                }
            })
        },
        SubTypes = a.SubTypes.Select(b => new DtoSubType
        {
            Id = b.Id,
            Name = b.Name,
            Money = b.Money.HasValue ? b.Money.Value : default
        })
    };

    public static DtoWithSubSelectsAndCollections From(EntityWithSubSelectsAndCollections entityWithSubSelectsAndCollections) => (selectEntityWithSubSelectsAndCollectionsFunc ??= SelectEntityWithSubSelectsAndCollectionsExpression.Compile())(entityWithSubSelectsAndCollections);
}

""";
}

file static class FooCode
{
    public const string Types =
"""
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Base
{
    public abstract class Base
    {
        public required int Id { get; set; }
    }

    public class Derived : Base
    {
        public required string Name { get; set; }
    }
}

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Foos
{
    using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Bars;
    using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Base;

    public class Foo : Derived
    {
        public string Description1 { get; set; } = null!;
        public string Description2 { get; set; }
        public string? Description3 { get; set; }
        public string? Description4 { get; set; }

        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int? Value3 { get; set; }
        public int? Value4 { get; set; }

        public IReadOnlyCollection<Bar> Bars1 { get; set; } = null!;
        public IReadOnlyCollection<Bar> Bars2 { get; set; }
        public IReadOnlyCollection<Bar>? Bars3 { get; set; }
        public IReadOnlyCollection<Bar>? Bars4 { get; set; }

        public IEnumerable<Baz> Bazs1 { get; set; } = null!;
        public IEnumerable<Baz> Bazs2 { get; set; }
        public IEnumerable<Baz>? Bazs3 { get; set; }
        public IEnumerable<Baz>? Bazs4 { get; set; }
    }

}

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Bars
{
    public class Bar
    {
        public Baz Baz1 { get; set; } = null!;
        public Baz Baz2 { get; set; } = null!;
        public Baz? Baz3 { get; set; } = null!;
        public Baz? Baz4 { get; set; } = null!;
    }
}

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Bazs
{
    public class Baz
    {
        public BazType Type1 { get; set; }
        public BazType Type2 { get; set; }
        public BazType? Type3 { get; set; }
        public BazType? Type4 { get; set; }

        public BazType Type5 { get; set; }
        public BazType Type6 { get; set; }
        public BazType? Type7 { get; set; }
        public BazType? Type8 { get; set; }
    }

    public enum BazType
    {
        Type1,
        Type2,
        Type3
    }
}

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Dtos
{
    using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Base;
    using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Bazs;
    using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Foos;

    [AutoSelect<Foo>]
    public partial class FooDto : Derived
    {
        public string Description1 { get; set; } = null!;
        public string? Description2 { get; set; }
        public string Description3 { get; set; } = null!;
        public string? Description4 { get; set; }

        public int Value1 { get; set; }
        public int? Value2 { get; set; }
        public int Value3 { get; set; }
        public int? Value4 { get; set; }

        public IReadOnlyCollection<BarDto> Bars1 { get; set; } = null!;
        public IReadOnlyCollection<BarDto>? Bars2 { get; set; }
        public IReadOnlyCollection<BarDto> Bars3 { get; set; } = null!;
        public IReadOnlyCollection<BarDto>? Bars4 { get; set; }

        public IEnumerable<BazDto> Bazs1 { get; set; } = null!;
        public IEnumerable<BazDto>? Bazs2 { get; set; }
        public IEnumerable<BazDto> Bazs3 { get; set; } = null!;
        public IEnumerable<BazDto>? Bazs4 { get; set; }
    }

    public class BarDto
    {
        public BazDto Baz1 { get; set; } = null!;
        public BazDto? Baz2 { get; set; } = null!;
        public BazDto Baz3 { get; set; } = null!;
        public BazDto? Baz4 { get; set; } = null!;
    }

    public class BazDto
    {
        public BazType Type1 { get; set; }
        public BazType? Type2 { get; set; }
        public BazType Type3 { get; set; }
        public BazType? Type4 { get; set; }

        public BazTypeDto Type5 { get; set; }
        public BazTypeDto? Type6 { get; set; }
        public BazTypeDto Type7 { get; set; }
        public BazTypeDto? Type8 { get; set; }
    }

    public enum BazTypeDto
    {
        Type1,
        Type2,
        Type3
    }
}
""";

    public const string ExpectedPartial =
"""
using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Bazs;
using RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Foos;
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.FooBarBaz.Dtos;

public partial class FooDto
{
    private static Func<Foo, FooDto> selectFooFunc;

    public static Expression<Func<Foo, FooDto>> SelectEntityWithSubSelectsAndCollectionsExpression { get; } = a => new FooDto
    {
        Id = a.Id,
        Name = a.Name,
        Description1 = a.Description1,
        Description2 = a.Description2,
        Description3 = a.Description3,
        Description4 = a.Description4,
        Value1 = a.Value1,
        Value2 = a.Value2,
        Value3 = a.Value3.HasValue ? a.Value3.Value : default,
        Value4 = a.Value4,
        Bars1 = a.Bars1.Select(b => new BarDto
        {
            Baz1 = new BazDto
            {
                Type1 = b.Baz1.Type1,
                Type2 = b.Baz1.Type2,
                Type3 = b.Baz1.Type3.HasValue ? b.Baz1.Type3.Value : default,
                Type4 = b.Baz1.Type4,
                Type5 = (BazTypeDto)b.Baz1.Type5,
                Type6 = (BazTypeDto?)b.Baz1.Type6,
                Type7 = (BazTypeDto)b.Baz1.Type7,
                Type8 = (BazTypeDto?)b.Baz1.Type8
            },
            Baz2 = new BazDto
            {
                Type1 = b.Baz2.Type1,
                Type2 = b.Baz2.Type2,
                Type3 = b.Baz2.Type3.HasValue ? b.Baz2.Type3.Value : default,
                Type4 = b.Baz2.Type4,
                Type5 = (BazTypeDto)b.Baz2.Type5,
                Type6 = (BazTypeDto?)b.Baz2.Type6,
                Type7 = (BazTypeDto)b.Baz2.Type7,
                Type8 = (BazTypeDto?)b.Baz2.Type8
            },
            Baz3 = new BazDto
            {
                Type1 = b.Baz3.Type1,
                Type2 = b.Baz3.Type2,
                Type3 = b.Baz3.Type3.HasValue ? b.Baz3.Type3.Value : default,
                Type4 = b.Baz3.Type4,
                Type5 = (BazTypeDto)b.Baz3.Type5,
                Type6 = (BazTypeDto?)b.Baz3.Type6,
                Type7 = (BazTypeDto)b.Baz3.Type7,
                Type8 = (BazTypeDto?)b.Baz3.Type8
            },
            Baz4 = new BazDto
            {
                Type1 = b.Baz4.Type1,
                Type2 = b.Baz4.Type2,
                Type3 = b.Baz4.Type3.HasValue ? b.Baz4.Type3.Value : default,
                Type4 = b.Baz4.Type4,
                Type5 = (BazTypeDto)b.Baz4.Type5,
                Type6 = (BazTypeDto?)b.Baz4.Type6,
                Type7 = (BazTypeDto)b.Baz4.Type7,
                Type8 = (BazTypeDto?)b.Baz4.Type8
            }
        }).ToList(),
        Bars2 = a.Bars2.Select(b => new BarDto
        {
            Baz1 = new BazDto
            {
                Type1 = b.Baz1.Type1,
                Type2 = b.Baz1.Type2,
                Type3 = b.Baz1.Type3.HasValue ? b.Baz1.Type3.Value : default,
                Type4 = b.Baz1.Type4,
                Type5 = (BazTypeDto)b.Baz1.Type5,
                Type6 = (BazTypeDto?)b.Baz1.Type6,
                Type7 = (BazTypeDto)b.Baz1.Type7,
                Type8 = (BazTypeDto?)b.Baz1.Type8
            },
            Baz2 = new BazDto
            {
                Type1 = b.Baz2.Type1,
                Type2 = b.Baz2.Type2,
                Type3 = b.Baz2.Type3.HasValue ? b.Baz2.Type3.Value : default,
                Type4 = b.Baz2.Type4,
                Type5 = (BazTypeDto)b.Baz2.Type5,
                Type6 = (BazTypeDto?)b.Baz2.Type6,
                Type7 = (BazTypeDto)b.Baz2.Type7,
                Type8 = (BazTypeDto?)b.Baz2.Type8
            },
            Baz3 = new BazDto
            {
                Type1 = b.Baz3.Type1,
                Type2 = b.Baz3.Type2,
                Type3 = b.Baz3.Type3.HasValue ? b.Baz3.Type3.Value : default,
                Type4 = b.Baz3.Type4,
                Type5 = (BazTypeDto)b.Baz3.Type5,
                Type6 = (BazTypeDto?)b.Baz3.Type6,
                Type7 = (BazTypeDto)b.Baz3.Type7,
                Type8 = (BazTypeDto?)b.Baz3.Type8
            },
            Baz4 = new BazDto
            {
                Type1 = b.Baz4.Type1,
                Type2 = b.Baz4.Type2,
                Type3 = b.Baz4.Type3.HasValue ? b.Baz4.Type3.Value : default,
                Type4 = b.Baz4.Type4,
                Type5 = (BazTypeDto)b.Baz4.Type5,
                Type6 = (BazTypeDto?)b.Baz4.Type6,
                Type7 = (BazTypeDto)b.Baz4.Type7,
                Type8 = (BazTypeDto?)b.Baz4.Type8
            }
        }).ToList(),
        Bars3 = a.Bars3.Select(b => new BarDto
        {
            Baz1 = new BazDto
            {
                Type1 = b.Baz1.Type1,
                Type2 = b.Baz1.Type2,
                Type3 = b.Baz1.Type3.HasValue ? b.Baz1.Type3.Value : default,
                Type4 = b.Baz1.Type4,
                Type5 = (BazTypeDto)b.Baz1.Type5,
                Type6 = (BazTypeDto?)b.Baz1.Type6,
                Type7 = (BazTypeDto)b.Baz1.Type7,
                Type8 = (BazTypeDto?)b.Baz1.Type8
            },
            Baz2 = new BazDto
            {
                Type1 = b.Baz2.Type1,
                Type2 = b.Baz2.Type2,
                Type3 = b.Baz2.Type3.HasValue ? b.Baz2.Type3.Value : default,
                Type4 = b.Baz2.Type4,
                Type5 = (BazTypeDto)b.Baz2.Type5,
                Type6 = (BazTypeDto?)b.Baz2.Type6,
                Type7 = (BazTypeDto)b.Baz2.Type7,
                Type8 = (BazTypeDto?)b.Baz2.Type8
            },
            Baz3 = new BazDto
            {
                Type1 = b.Baz3.Type1,
                Type2 = b.Baz3.Type2,
                Type3 = b.Baz3.Type3.HasValue ? b.Baz3.Type3.Value : default,
                Type4 = b.Baz3.Type4,
                Type5 = (BazTypeDto)b.Baz3.Type5,
                Type6 = (BazTypeDto?)b.Baz3.Type6,
                Type7 = (BazTypeDto)b.Baz3.Type7,
                Type8 = (BazTypeDto?)b.Baz3.Type8
            },
            Baz4 = new BazDto
            {
                Type1 = b.Baz4.Type1,
                Type2 = b.Baz4.Type2,
                Type3 = b.Baz4.Type3.HasValue ? b.Baz4.Type3.Value : default,
                Type4 = b.Baz4.Type4,
                Type5 = (BazTypeDto)b.Baz4.Type5,
                Type6 = (BazTypeDto?)b.Baz4.Type6,
                Type7 = (BazTypeDto)b.Baz4.Type7,
                Type8 = (BazTypeDto?)b.Baz4.Type8
            }
        }).ToList(),
        Bars4 = a.Bars4.Select(b => new BarDto
        {
            Baz1 = new BazDto
            {
                Type1 = b.Baz1.Type1,
                Type2 = b.Baz1.Type2,
                Type3 = b.Baz1.Type3.HasValue ? b.Baz1.Type3.Value : default,
                Type4 = b.Baz1.Type4,
                Type5 = (BazTypeDto)b.Baz1.Type5,
                Type6 = (BazTypeDto?)b.Baz1.Type6,
                Type7 = (BazTypeDto)b.Baz1.Type7,
                Type8 = (BazTypeDto?)b.Baz1.Type8
            },
            Baz2 = new BazDto
            {
                Type1 = b.Baz2.Type1,
                Type2 = b.Baz2.Type2,
                Type3 = b.Baz2.Type3.HasValue ? b.Baz2.Type3.Value : default,
                Type4 = b.Baz2.Type4,
                Type5 = (BazTypeDto)b.Baz2.Type5,
                Type6 = (BazTypeDto?)b.Baz2.Type6,
                Type7 = (BazTypeDto)b.Baz2.Type7,
                Type8 = (BazTypeDto?)b.Baz2.Type8
            },
            Baz3 = new BazDto
            {
                Type1 = b.Baz3.Type1,
                Type2 = b.Baz3.Type2,
                Type3 = b.Baz3.Type3.HasValue ? b.Baz3.Type3.Value : default,
                Type4 = b.Baz3.Type4,
                Type5 = (BazTypeDto)b.Baz3.Type5,
                Type6 = (BazTypeDto?)b.Baz3.Type6,
                Type7 = (BazTypeDto)b.Baz3.Type7,
                Type8 = (BazTypeDto?)b.Baz3.Type8
            },
            Baz4 = new BazDto
            {
                Type1 = b.Baz4.Type1,
                Type2 = b.Baz4.Type2,
                Type3 = b.Baz4.Type3.HasValue ? b.Baz4.Type3.Value : default,
                Type4 = b.Baz4.Type4,
                Type5 = (BazTypeDto)b.Baz4.Type5,
                Type6 = (BazTypeDto?)b.Baz4.Type6,
                Type7 = (BazTypeDto)b.Baz4.Type7,
                Type8 = (BazTypeDto?)b.Baz4.Type8
            }
        }).ToList()
    };
}

""";
}