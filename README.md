# SmartSelector

Gerador/Source Generator para criar automaticamente projeções (`Expression<Func<TFrom, TDto>>`),
métodos auxiliares e propriedades em DTOs, reduzindo drasticamente boilerplate em consultas LINQ / EF Core.

## Principais Recursos
- `[AutoSelect<TFrom>]`: gera expressão de seleção, método `From`, extensões `Select{Dto}` / `To{Dto}`.
- `[AutoProperties]` ou `[AutoProperties<TFrom>]`: gera propriedades simples automaticamente (primitivos, string, bool, DateTime, enum, struct, coleções simples `IEnumerable<T>` desses tipos).
- Flattening por convenção: nomes concatenados em PascalCase resolvem cadeias aninhadas (ex.: `CustomerAddressCountryRegionName` ? `a.Customer.Address.Country.Region.Name`).
- Exclusão de propriedades: `Exclude = [ nameof(Entity.Prop) ]`.
- Diagnósticos de compilação para uso incorreto, tipos incompatíveis e conflitos.

## Quickstart

1) Instalação
```xml
<ItemGroup>
  <PackageReference Include="RoyalCode.SmartSelector" Version="x.y.z" />
  <PackageReference Include="RoyalCode.SmartSelector.Generators" Version="x.y.z" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

2) Anote seu DTO
```csharp
[AutoSelect<User>, AutoProperties]
public partial class UserDetails { }
```

3) Consulte com EF Core
```csharp
var list = db.Users.SelectUserDetails().ToList();
var dto  = UserDetails.From(user);
var expr = UserDetails.SelectUserExpression; // reutilizável / componível
```

Escopo e foco
- Sem custom resolvers, conditional mapping ou naming policies.
- Foco: projeções traduzíveis por EF Core e mapeamento 1x1 (estilo Adapt do Mapster).

Links úteis
- Documentação completa: `src/docs.md`
- Projeto Demo: `RoyalCode.SmartSelector.Demo`
- Benchmarks: `RoyalCode.SmartSelector.Benchmarks`

Frameworks e pacotes suportados
- Runtime lib: `RoyalCode.SmartSelector` (TFMs: .NET 8, .NET 9, .NET 10)
- Generator: `RoyalCode.SmartSelector.Generators` (TFM: .NET Standard 2.0, instalado como Analyzer)

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
public static Expression<Func<User, UserDetails>> SelectUserExpression => u => new UserDetails
{ 
    Id = u.Id, 
    Name = u.Name
};

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
new BookDetails
{
    Shelf = new ShelfDetails
    {
        Id = a.Shelf.Id,
        Location = a.Shelf.Location
    },
    Price = a.Price,
};
// Sku excluído
```

## Exemplo 3 – Flattening Profundo
```csharp
public class Order 
{ 
    public Customer Customer { get; set; }
}

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

## Exemplos mínimos por atributo

- `AutoSelect<T>`:

```csharp
[AutoSelect<Product>]
public partial class ProductDetails { }

// Uso:
db.Products.SelectProductDetails().ToList();
```

- `AutoProperties`:

```csharp
[AutoSelect<Simple>, AutoProperties]
public partial class SimpleDto { /* propriedades simples geradas automaticamente */ }
```

Para usar `AutoProperties`, o tipo de origem é inferido do `AutoSelect<TFrom>`.

- `AutoProperties<TFrom>`:

```csharp
[AutoProperties<User>]
public partial class UserSnapshot { }
```

- DTO aninhado + `Exclude`:

```csharp
[AutoSelect<Order>, AutoProperties<Order>(Exclude = [ nameof(Order.InternalCode) ])]
public partial class OrderDetails 
{
    public CustomerDetails Customer { get; set; }
}

[AutoProperties<Customer>]
public partial class CustomerDetails { }
```

- Flattening por nome:
```csharp
[AutoSelect<Order>]
public partial class OrderFlat 
{ 
    public string CustomerAddressCity { get; set; }
}

// Gera: CustomerAddressCity = a.Customer.Address.City
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
- Documentação detalhada: ver `docs.md` no repositório.
- Projeto Demo: ver `RoyalCode.SmartSelector.Demo`.
- Benchmarks: ver `RoyalCode.SmartSelector.Benchmarks`.

---
Happy coding!
