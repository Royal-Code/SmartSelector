# SmartSelector

Gerador/Source Generator para criar automaticamente projeções (`Expression<Func<TFrom, TDto>>`), métodos auxiliares e propriedades em DTOs, reduzindo drasticamente boilerplate em consultas LINQ / EF Core.

## Principais Recursos
- `[AutoSelect<TFrom>]`: gera expressão de seleção, método `From`, extensões `Select{Dto}` / `To{Dto}`.
- `[AutoProperties]` ou `[AutoProperties<TFrom>]`: gera propriedades simples automaticamente (primitivos, string, bool, DateTime, enum, struct, coleções simples `IEnumerable<T>` desses tipos).
- Flattening por convenção: nomes concatenados em PascalCase resolvem cadeias aninhadas (ex.: `CustomerAddressCountryRegionName` ? `a.Customer.Address.Country.Region.Name`).
- Exclusão de propriedades: `Exclude = [ nameof(Entity.Prop) ]`.
- Diagnósticos de compilação para uso incorreto, tipos incompatíveis e conflitos.

## Instalação
```xml
<ItemGroup>
  <PackageReference Include="RoyalCode.SmartSelector" Version="x.y.z" />
  <PackageReference Include="RoyalCode.SmartSelector.Generators" Version="x.y.z" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Exemplo 1 – Projeção Simples
```csharp
[AutoSelect<User>, AutoProperties]
public partial class UserDetails { }

// Uso
var list = db.Users.SelectUserDetails().ToList();
var dto  = UserDetails.From(user);
var expr = UserDetails.SelectUserExpression; // reutilizável / componível
```
Código gerado (essencial):
```csharp
public static Expression<Func<User, UserDetails>> SelectUserExpression => u => new UserDetails { Id = u.Id, Name = u.Name };
public static UserDetails From(User u) => (selectUserFunc ??= SelectUserExpression.Compile())(u);
```

## Exemplo 2 – Objeto Aninhado + Exclusão
```csharp
[AutoSelect<Book>, AutoProperties(Exclude = [ nameof(Book.Sku) ])]
public partial class BookDetails
{
    public ShelfDetails Shelf { get; set; }
}

[AutoProperties<Shelf>]
public partial class ShelfDetails { }
```
Trecho gerado:
```csharp
Shelf = new ShelfDetails { Id = a.Shelf.Id, Location = a.Shelf.Location },
Price = a.Price,
// Sku excluído
```

## Exemplo 3 – Flattening Profundo
```csharp
public class Order { public Customer Customer { get; set; } }
// Customer -> Address -> Country -> Region
[AutoSelect<Order>]
public partial class OrderDetails
{
    public string CustomerAddressCountryRegionName { get; set; }
}
```
Trecho da expressão:
```csharp
CustomerAddressCountryRegionName = a.Customer.Address.Country.Region.Name
```

## Regras de Flattening
- Nome da propriedade = concatenação PascalCase dos segmentos do caminho.
- Sem necessidade de atributos extras.

## Tipos Suportados em AutoProperties
- Primitivos numéricos, `bool`, `string`, `char`, `DateTime` / nullable simples
- `enum`, `struct`
- `IEnumerable<T>` onde `T` é suportado acima / enum / struct

## Exclusões
```csharp
[AutoProperties<Product>(Exclude = [ nameof(Product.InternalCode), nameof(Product.Secret) ])]
```

## Diagnósticos Principais
- Tipos inválidos ou classe não `partial` (`RCSS000`).
- Propriedade não encontrada (`RCSS001`).
- Tipos incompatíveis (`RCSS002`).
- Uso incorreto de atributos (`RCSS003`–`RCSS005`).

## Limitações Resumidas
- Sem renome/alias explícito ainda (`MapFrom`).
- Sem transformações de tipo (formatters / custom converters).
- Desambiguação de flattening limitada em colisões de prefixo.

## Boas Práticas
- Use `nameof` em `Exclude`.
- Prefira consumir a expressão gerada para reutilização e composição LINQ.
- Para caminhos muito longos, avalie DTOs aninhados por clareza.

## FAQ Rápido
| Pergunta | Resposta |
|----------|----------|
| Preciso configurar algo no runtime? | Não, pura geração de código. |
| Funciona com EF Core? | Sim, a expressão é traduzível. |
| Posso só gerar propriedades? | Sim: `[AutoProperties<TFrom>]`. |
| Flattening precisa de atributo? | Não, é por nome. |

## Mais Informações
Documentação detalhada: ver `docs.md` no repositório.

---
Happy coding!
