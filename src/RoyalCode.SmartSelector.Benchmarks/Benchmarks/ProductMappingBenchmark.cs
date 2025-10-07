using BenchmarkDotNet.Attributes;
using RoyalCode.SmartSelector.Benchmarks.Models;
using AutoMapper;
using Mapster;

namespace RoyalCode.SmartSelector.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ProductMappingBenchmark
{
    private List<Product> _source = default!;
    private IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;
    private Func<Product, ProductDetails> _mapsterFunc = null!;

    [Params(10, 1_000, 10_000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        var now = DateTime.UtcNow;
        _source = Enumerable.Range(1, Count)
            .Select(i => new Product("Product " + i))
            .ToList();

        // AutoMapper configuration
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.CreateMap<Product, ProductDetails>(),
            Logger.CreateLoggerFactory());
        _autoMapper = mapperConfig.CreateMapper();

        // Mapster configuration
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<Product, ProductDetails>();
        _mapsterFunc = _mapsterConfig.GetMapFunction<Product, ProductDetails>();

        // Force JIT warmup of generated delegate:
        _ = ProductDetails.From(_source[0]);
        _ = _autoMapper.Map<ProductDetails>(_source[0]);
        _ = _mapsterFunc(_source[0]);
    }

    [Benchmark(Baseline = true, Description = "Manual new ProductDetails")]
    public List<ProductDetails> Manual()
        => _source.Select(p => new ProductDetails
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

    [Benchmark(Description = "Generated From()")]
    public List<ProductDetails> Generated_From()
        => _source.SelectProductDetails().ToList();

    [Benchmark(Description = "Generated compiled delegate (cache hit)")]
    public List<ProductDetails> Generated_CachedDelegate()
    {
        // Simulate direct usage of cached compiled lambda if exposed.
        // From() already uses the cached delegate; included for clarity.
        return _source.SelectProductDetails().ToList();
    }

    [Benchmark(Description = "AutoMapper ProjectTo List")]
    public List<ProductDetails> AutoMapper_Map()
        => _source.Select(p => _autoMapper.Map<ProductDetails>(p)).ToList();

    [Benchmark(Description = "Mapster Adapt List")]
    public List<ProductDetails> Mapster_Adapt()
        => _source.Select(p => _mapsterFunc(p)).ToList();
}