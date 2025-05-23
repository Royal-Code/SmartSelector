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
