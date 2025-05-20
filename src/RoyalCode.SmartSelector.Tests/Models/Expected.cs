
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
    private static Func<Produto, ProdutoDetalhes> selectProdutoFunc;

    public static Expression<Func<Produto, ProdutoDetalhes>> SelectProdutoExpression { get; } = p => new ProdutoDetalhes
    {
        Id = p.Id,
        Nome = p.Nome,
        Ativo = p.Ativo
    };

    public static ProdutoDetalhes From(Produto produto) => (selectProdutoFunc ??= SelectProdutoExpression.Compile())(produto);
}

// Generated code
public static class ProdutoDetalhes_Extensions
{
    public static IQueryable<ProdutoDetalhes> SelectProdutoDetalhes(this IQueryable<Produto> produtos)
    {
        return produtos.Select(ProdutoDetalhes.SelectProdutoExpression);
    }

    public static IEnumerable<ProdutoDetalhes> SelectProdutoDetalhes(this IEnumerable<Produto> produtos)
    {
        return produtos.Select(ProdutoDetalhes.From);
    }
}