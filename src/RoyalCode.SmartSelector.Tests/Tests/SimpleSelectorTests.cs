using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class SimpleSelectorTests
{
    [Fact]
    public void Select_ProdutoDetalhes()
    {
        Util.Compile(Code.Types, out var output, out var diagnostics);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        var generatedInterface = output.SyntaxTrees.Skip(1).FirstOrDefault()?.ToString();
        generatedInterface.Should().Be(Code.ExpectedPartial);

        var generatedHandler = output.SyntaxTrees.Skip(2).FirstOrDefault()?.ToString();
        generatedHandler.Should().Be(Code.ExpectedExtension);
    }
}

file static class Code
{
    public const string Types =
"""
using RoyalCode.SmartSelector;

namespace Tests.SmartSelector.Models;

#nullable disable // poco

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
}

public class Produto : Entity<Guid>
{
    public Produto(string nome)
    {
        Nome = nome;
        Ativo = true;
    }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
}

[AutoSelect<Produto>]
public partial class ProdutoDetalhes
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
}
""";

    public const string ExpectedPartial =
"""
using System.Linq.Expressions;

namespace Tests.SmartSelector.Models;

public partial class ProdutoDetalhes
{
    private static readonly Expression<Func<Produto, ProdutoDetalhes>> selectExpression = p => new ProdutoDetalhes
    {
        Id = p.Id,
        Nome = p.Nome,
        Ativo = p.Ativo
    };

    private static readonly Func<Produto, ProdutoDetalhes> selectFunc = selectExpression.Compile();

    public static Expression<Func<Produto, ProdutoDetalhes>> SelectExpression => selectExpression;

    public static ProdutoDetalhes From(Produto produto) => selectFunc(produto);
}
""";

    public const string ExpectedExtension =
"""
using System.Linq.Expressions;

using System.Collections.Generic;

public static class ProdutoDetalhes_Extensions
{
    public static IQueryable<ProdutoDetalhes> Select(this IQueryable<Produto> produtos)
    {
        return produtos.Select(ProdutoDetalhes.SelectExpression);
    }

    public static IEnumerable<ProdutoDetalhes> Select(this IEnumerable<Produto> produtos)
    {
        return produtos.Select(ProdutoDetalhes.From);
    }
}
""";
}