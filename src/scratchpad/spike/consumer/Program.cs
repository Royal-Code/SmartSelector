namespace SpikeConsumer
{
    using RoyalCode.SmartSelector;

    public static class Program
    {
        public static void Main()
        {
            var details = ProductDetails.From(new Product { Id = 7, Name = "Spike" });
            Console.WriteLine($"OK: {details.Id} {details.Name}");
        }
    }

    public sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [AutoSelect<Product>]
    public partial class ProductDetails
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

namespace RoyalCode.SmartSelector
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoSelectAttribute<TFrom> : Attribute;
}
