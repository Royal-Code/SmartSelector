# SmartSelector

Gerador/Source Generator para criar automaticamente proje��es (`Expression<Func<TFrom, TDto>>`), m�todos auxiliares e propriedades em DTOs, reduzindo drasticamente boilerplate em consultas LINQ / EF Core.

## Principais Recursos
- `[AutoSelect<TFrom>]`: gera express�o de sele��o, m�todo `From`, extens�es `Select{Dto}` / `To{Dto}`.
- `[AutoProperties]` ou `[AutoProperties<TFrom>]`: gera propriedades simples automaticamente (primitivos, string, bool, DateTime, enum, struct, cole��es simples `IEnumerable<T>` desses tipos).
- Flattening por conven��o: nomes concatenados em PascalCase resolvem cadeias aninhadas (ex.: `CustomerAddressCountryRegionName` ? `a.Customer.Address.Country.Region.Name`).
- Exclus�o de propriedades: `Exclude = [ nameof(Entity.Prop) ]`.
- Diagn�sticos de compila��o para uso incorreto, tipos incompat�veis e conflitos.

## Instala��o
```xml
<ItemGroup>
  <PackageReference Include="RoyalCode.SmartSelector" Version="x.y.z" />
  <PackageReference Include="RoyalCode.SmartSelector.Generators" Version="x.y.z" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Exemplo 1 � Proje��o Simples
```csharp
[AutoSelect<User>, AutoProperties]
public partial class UserDetails { }

// Uso
var list = db.Users.SelectUserDetails().ToList();
var dto  = UserDetails.From(user);
var expr = UserDetails.SelectUserExpression; // reutiliz�vel / compon�vel
```
C�digo gerado (essencial):
```csharp
public static Expression<Func<User, UserDetails>> SelectUserExpression => u => new UserDetails { Id = u.Id, Name = u.Name };
public static UserDetails From(User u) => (selectUserFunc ??= SelectUserExpression.Compile())(u);
```

## Exemplo 2 � Objeto Aninhado + Exclus�o
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
// Sku exclu�do
```

## Exemplo 3 � Flattening Profundo
```csharp
public class Order { public Customer Customer { get; set; } }
// Customer -> Address -> Country -> Region
[AutoSelect<Order>]
public partial class OrderDetails
{
    public string CustomerAddressCountryRegionName { get; set; }
}
```
Trecho da express�o:
```csharp
CustomerAddressCountryRegionName = a.Customer.Address.Country.Region.Name
```

## Regras de Flattening
- Nome da propriedade = concatena��o PascalCase dos segmentos do caminho.
- Sem necessidade de atributos extras.

## Tipos Suportados em AutoProperties
- Primitivos num�ricos, `bool`, `string`, `char`, `DateTime` / nullable simples
- `enum`, `struct`
- `IEnumerable<T>` onde `T` � suportado acima / enum / struct

## Exclus�es
```csharp
[AutoProperties<Product>(Exclude = [ nameof(Product.InternalCode), nameof(Product.Secret) ])]
```

## Diagn�sticos Principais
- Tipos inv�lidos ou classe n�o `partial` (`RCSS000`).
- Propriedade n�o encontrada (`RCSS001`).
- Tipos incompat�veis (`RCSS002`).
- Uso incorreto de atributos (`RCSS003`�`RCSS005`).

## Limita��es Resumidas
- Sem renome/alias expl�cito ainda (`MapFrom`).
- Sem transforma��es de tipo (formatters / custom converters).
- Desambigua��o de flattening limitada em colis�es de prefixo.

## Boas Pr�ticas
- Use `nameof` em `Exclude`.
- Prefira consumir a express�o gerada para reutiliza��o e composi��o LINQ.
- Para caminhos muito longos, avalie DTOs aninhados por clareza.

## FAQ R�pido
| Pergunta | Resposta |
|----------|----------|
| Preciso configurar algo no runtime? | N�o, pura gera��o de c�digo. |
| Funciona com EF Core? | Sim, a express�o � traduz�vel. |
| Posso s� gerar propriedades? | Sim: `[AutoProperties<TFrom>]`. |
| Flattening precisa de atributo? | N�o, � por nome. |

## Mais Informa��es
Documenta��o detalhada: ver `docs.md` no reposit�rio.

---
Happy coding!
