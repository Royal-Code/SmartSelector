using BenchmarkDotNet.Attributes;
using RoyalCode.SmartSelector.Benchmarks.Models;
using AutoMapper.QueryableExtensions;
using Mapster;

namespace RoyalCode.SmartSelector.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ProductQueryableBenchmark
{
    private IQueryable<Product> _queryable = default!;
    private AutoMapper.IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;

    [Params(10, 1_000, 10_000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        var data = Enumerable.Range(1, Count)
            .Select(i => new Product("Product " + i))
            .ToList();

        _queryable = data.AsQueryable();

        // AutoMapper configuration
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.CreateMap<Product, ProductDetails>());
        _autoMapper = mapperConfig.CreateMapper();

        // Mapster configuration for projection
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<Product, ProductDetails>();

        // warmup
        _ = _queryable.SelectProductDetails().First();
        _ = _queryable.Select(p => _autoMapper.Map<ProductDetails>(p)).First();
        _ = _queryable.ProjectTo<ProductDetails>(_autoMapper.ConfigurationProvider).First();
        _ = _queryable.Select(p => p.Adapt<ProductDetails>()).First();
    }

    [Benchmark(Baseline = true, Description = "Manual expression")]
    public int ManualExpression()
    {
        return _queryable.Select(p => new ProductDetails
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).Count();
    }

    [Benchmark(Description = "Generated expression")]
    public int GeneratedExpression()
        => _queryable.SelectProductDetails().Count();

    [Benchmark(Description = "AutoMapper Map() in Select")]
    public int AutoMapper_MapInSelect()
        => _queryable.Select(p => _autoMapper.Map<ProductDetails>(p)).Count();

    [Benchmark(Description = "AutoMapper ProjectTo()")]
    public int AutoMapper_ProjectTo()
        => _queryable.ProjectTo<ProductDetails>(_autoMapper.ConfigurationProvider).Count();

    [Benchmark(Description = "Mapster Adapt() in Select")]
    public int Mapster_AdaptInSelect()
        => _queryable.Select(p => p.Adapt<ProductDetails>()).Count();
}