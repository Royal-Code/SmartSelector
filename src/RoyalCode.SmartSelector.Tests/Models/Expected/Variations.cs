using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

#nullable disable

public class Color : Entity<long>
{
    public Color(string name, string hexCode)
    {
        Name = name;
        HexCode = hexCode;
    }
    public string Name { get; set; }
    
    public string HexCode { get; set; }
}

public class Size : Entity<long>
{
    public Size(string name, string code)
    {
        Name = name;
        Code = code;
    }

    public string Name { get; set; }
    
    public string Code { get; set; }
}

public class Variation : Entity<long>
{
    public Variation(Product product, string name, string code, Color color, Size size)
    {
        Id = Random.Shared.NextInt64(1, 1000);
        Product = product;
        Name = name;
        Code = code;
        Color = color;
        Size = size;
    }

    public Product Product { get; }

    public string Name { get; set; }
    
    public string Code { get; set; }
    
    public Color Color { get; set; }
    
    public Size Size { get; set; }

    public bool Active { get; set; } = true;
}

[AutoSelect<Variation>]
public partial class VariationDetails
{
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string Code { get; set; }
    
    public string ColorName { get; set; }
    
    public string SizeCode { get; set; }
}

// expected

public partial class VariationDetails
{
    private static Func<Variation, VariationDetails> selectVariationFunc;

    public static Expression<Func<Variation, VariationDetails>> SelectVariationExpression { get; } = a => new VariationDetails
    {
        Id = a.Id,
        Name = a.Name,
        Code = a.Code,
        ColorName = a.Color.Name,
        SizeCode = a.Size.Code
    };

    public static VariationDetails From(Variation variation) => (selectVariationFunc ??= SelectVariationExpression.Compile())(variation);
}