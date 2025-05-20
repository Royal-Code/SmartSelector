namespace RoyalCode.SmartSelector.Tests.Models.Expected;

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
