namespace RoyalCode.SmartSelector.Tests.Models.Expected;

// Generated code
public static class ProdutoDetalhes_Extensions
{
    public static IQueryable<ProdutoDetalhes> SelectProdutoDetalhes(this IQueryable<Produto> query)
    {
        return query.Select(ProdutoDetalhes.SelectProdutoExpression);
    }

    public static IEnumerable<ProdutoDetalhes> SelectProdutoDetalhes(this IEnumerable<Produto> enumerable)
    {
        return enumerable.Select(ProdutoDetalhes.From);
    }

    public static ProdutoDetalhes ToProdutoDetalhes(this Produto produto) => ProdutoDetalhes.From(produto);
}