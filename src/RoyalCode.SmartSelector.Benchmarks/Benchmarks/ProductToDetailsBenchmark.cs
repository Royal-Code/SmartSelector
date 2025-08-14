using BenchmarkDotNet.Attributes;
using RoyalCode.SmartSelector.Benchmarks.Models;
using AutoMapper;
using Mapster;

namespace RoyalCode.SmartSelector.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ProductToDetailsBenchmark
{
    private Product product = null!;
    private IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;
    private Func<Product, ProductDetails> _mapsterFunc = null!;

    [Params(10, 100, 1_000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        product = new Product("Sample Product");

        // AutoMapper configuration
        var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<Product, ProductDetails>());
        _autoMapper = mapperConfig.CreateMapper();

        // Mapster configuration
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<Product, ProductDetails>();
        _mapsterFunc = _mapsterConfig.GetMapFunction<Product, ProductDetails>();

        // Force JIT warmup of generated delegate:
        _ = ProductDetails.From(product);
        _ = _autoMapper.Map<ProductDetails>(product);
        _ = _mapsterFunc(product);
    }

    [Benchmark(Baseline = true, Description = "Manual new ProductDetails")]
    public ProductDetails Manual()
    {
        return new ProductDetails
        {
            Id = product.Id,
            Name = product.Name,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    [Benchmark(Description = "Generated From()")]
    public ProductDetails Generated_From() => ProductDetails.From(product);

    [Benchmark(Description = "Generated ToProductDetails()")]
    public ProductDetails Generated_ToProductDetails() => product.ToProductDetails();

    [Benchmark(Description = "AutoMapper Map()")]
    public ProductDetails AutoMapper_Map() => _autoMapper.Map<ProductDetails>(product);

    [Benchmark(Description = "Mapster Adapt()")]
    public ProductDetails Mapster_Adapt() => _mapsterFunc(product);
}
