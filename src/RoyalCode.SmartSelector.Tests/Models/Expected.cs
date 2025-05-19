
using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models;

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

// Generated code
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

// Generated code
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