using System.Linq.Expressions;

namespace RoyalCode.SmartSelector.Tests.Models.Expected;

// Generated code
public partial class ProdutoDetalhes
{
    private static Func<Produto, ProdutoDetalhes> selectProdutoFunc;

    public static Expression<Func<Produto, ProdutoDetalhes>> SelectProdutoExpression { get; } = a => new ProdutoDetalhes
    {
        Id = a.Id,
        Nome = a.Nome,
        Ativo = a.Ativo
    };

    public static ProdutoDetalhes From(Produto produto) => (selectProdutoFunc ??= SelectProdutoExpression.Compile())(produto);
}
