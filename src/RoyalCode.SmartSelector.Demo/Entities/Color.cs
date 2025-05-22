namespace RoyalCode.SmartSelector.Demo.Entities;

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
