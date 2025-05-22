namespace RoyalCode.SmartSelector.Demo.Entities;

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
