# SmartSelector

Gerador de código (Roslyn Source Generator) para criação de projeções (DTOs) e propriedades automáticas fortemente tipadas, reduzindo boilerplate em consultas LINQ / EF Core e mapeamentos simples.

---
## Sumário
1. Visão Geral
2. Exemplos Rápidos (Quick Start)
3. Conceitos Principais
4. Atributos Disponíveis
5. Membros Gerados
6. Propriedades Suportadas e Filtragem
7. Exclusão de Propriedades
8. Flattening (Projeção de Caminhos Aninhados)
9. MapFrom (Alias explícito de origem)
 10. FAQ Rápido

---
## 1. Visão Geral
O SmartSelector gera, a partir de classes decoradas com atributos, código auxiliar para:
- Construir expressões de seleção reutilizáveis: `Expression<Func<TFrom, TModel>>`.
- Gerar métodos de fábrica: `From(TFrom source)`.
- Gerar métodos de extensão para `IQueryable<TFrom>` e `IEnumerable<TFrom>`.
- Gerar automaticamente propriedades simples em DTOs a partir de um tipo de origem.
- Realizar flattening (achatar) de propriedades aninhadas por convenção de nome.

---
## 2. Exemplos Rápidos (Quick Start)
A ideia é: você cria uma classe parcial vazia (ou quase) com atributos e o gerador cria o resto.

### 2.1 Projeção simples + propriedades automáticas
```csharp
[AutoSelect<User>, AutoProperties]
public partial class UserDetails { }
```
Uso:
```csharp
var list = db.Users.SelectUserDetails().ToList();
var expr = UserDetails.SelectUserExpression;
var dto  = UserDetails.From(existingUser);
```
Código gerado (essencial):
```csharp
public partial class UserDetails
{
    private static Func<User, UserDetails> selectUserFunc;

    public static Expression<Func<User, UserDetails>> SelectUserExpression => u => new UserDetails
    {
        Id = u.Id,
        Name = u.Name,
        Status = (int)u.Status,
        // ... outras propriedades simples copiadas
    };

    public static UserDetails From(User user) => (selectUserFunc ??= SelectUserExpression.Compile())(user);
}

public static class UserDetails_Extensions
{
    public static IQueryable<UserDetails> SelectUserDetails(this IQueryable<User> query)
        => query.Select(UserDetails.SelectUserExpression);

    public static IEnumerable<UserDetails> SelectUserDetails(this IEnumerable<User> enumerable)
        => enumerable.Select(UserDetails.From);

    public static UserDetails ToUserDetails(this User user) => UserDetails.From(user);
}
```

### 2.2 Projeção com objeto aninhado e exclusão
```csharp
[AutoSelect<Book>, AutoProperties(Exclude = [ nameof(Book.Sku) ])]
public partial class BookDetails
{
    public ShelfDetails Shelf { get; set; } // Navegação explicitamente declarada
}

[AutoProperties<Shelf>]
public partial class ShelfDetails { }
```
Uso:
```csharp
var details = db.Books.Select(BookDetails.SelectBookExpression).ToList();
var single  = BookDetails.From(bookInstance);
```
Código gerado (resumo real):
```csharp
public partial class BookDetails
{
    private static Func<Book, BookDetails> selectBookFunc;

    public static Expression<Func<Book, BookDetails>> SelectBookExpression => a => new BookDetails
    {
        Shelf = new ShelfDetails
        {
            Id = a.Shelf.Id,
            Location = a.Shelf.Location
        },
        Id = a.Id,
        Title = a.Title,
        Author = a.Author,
        PublishedDate = a.PublishedDate,
        ISBN = a.ISBN,
        Price = a.Price,
        InStock = a.InStock
        // Sku excluído
    };

    public static BookDetails From(Book book) => (selectBookFunc ??= SelectBookExpression.Compile())(book);
}

public partial class BookDetails // propriedades auto (arquivo separado *.AutoProperties.g.cs)
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public DateTime PublishedDate { get; set; }
    public string ISBN { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public partial class ShelfDetails // gerado de AutoProperties<Shelf>
{
    public Guid Id { get; set; }
    public string Location { get; set; }
}
```

### 2.3 Somente propriedades automáticas
```csharp
[AutoProperties<Product>]
public partial class ProductSnapshot { }
```
Código gerado (exemplo):
```csharp
public partial class ProductSnapshot
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    // ... mais propriedades simples
}
```

### 2.4 Flattening simples
Dado:
```csharp
public class Customer { public Address Address { get; set; } }
public class Address  { public string City { get; set; } }

[AutoSelect<Customer>]
public partial class CustomerDetails
{
    public string AddressCity { get; set; } // mapeia Address.City
}
```
Expressão gerada (trecho):
```csharp
AddressCity = a.Address.City
```

### 2.5 Flattening profundo (multi-nível)
Dado (cadeia Customer -> Address -> Country -> Region):
```csharp
public class Region  { public string Name { get; set; } }
public class Country { public string Name { get; set; } public Region Region { get; set; } }
public class Address { public Country Country { get; set; } }
public class Customer { public Address Address { get; set; } }
public class Order { public Customer Customer { get; set; } }

[AutoSelect<Order>]
public partial class OrderDetails
{
    public string CustomerAddressCountryName { get; set; }
    public string CustomerAddressCountryRegionName { get; set; }
}
```
Expressão gerada (trecho real testado):
```csharp
CustomerAddressCountryName = a.Customer.Address.Country.Name,
CustomerAddressCountryRegionName = a.Customer.Address.Country.Region.Name
```
A convenção concatena os identificadores do caminho em PascalCase.

---
## 3. Conceitos Principais

| Conceito | Descrição |
|----------|----------|
| `Model / Details` | Classe parcial alvo da projeção (DTO). |
| `TFrom` | Tipo origem da projeção. |
| `AutoSelect<TFrom>` | Ativa geração de expressão + extensões. |
| `AutoProperties` / `AutoProperties<TFrom>` | Geração automática de propriedades simples. |
| Flattening | Casamento por convenção de nomes concatenados para acessar membros aninhados. |
| `MapFrom` | Mapeia explicitamente a origem de uma propriedade do DTO usando nome de membro da origem. |

---
## 4. Atributos Disponíveis
Mesmos comportamentos já descritos anteriormente (`AutoSelect<TFrom>`, `AutoProperties`, `AutoProperties<TFrom>`, `Exclude`, `MapFrom`).

### 4.1 `MapFromAttribute`
Permite definir diretamente de qual propriedade de `TFrom` uma propriedade do DTO será mapeada, sem depender de convenções de nome (flattening) ou posição.

Assinatura:
```csharp
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string propertyName) { PropertyName = propertyName; }
    public string PropertyName { get; set; }
}
```

Uso:
```csharp
[AutoSelect<Product>]
public partial class CustomProductDetails
{
    [MapFrom("Id")]                   // literal
    public Guid CustomId { get; set; }

    [MapFrom(nameof(Product.Name))]    // nameof evita typos
    public string CustomName { get; set; }

    [MapFrom(nameof(Product.Active))]
    public bool CustomActive { get; set; }
}
```

Expressão gerada (trecho real):
```csharp
new CustomProductDetails
{
    CustomId = a.Id,
    CustomName = a.Name,
    CustomActive = a.Active
}
```

---
## 5. Membros Gerados

| Membro | Descrição |
|--------|----------|
| `Select{TFrom}Expression` | Expressão de projeção. |
| `From` | Constrói instância usando expressão compilada (cache). |
| `Select{Model}` (IQueryable/Enumerable) | Métodos de extensão de projeção. |
| `To{Model}` | Conversão direta de instância única. |
| Propriedades auto | Adicionadas quando `AutoProperties` está presente. |

---
## 6. Propriedades Suportadas e Filtragem
Mesmos critérios: primitivos, string, bool, DateTime, enum, struct e coleções simples (`IEnumerable<T>` de tipo suportado).

---
## 7. Exclusão de Propriedades
Via `Exclude = [ nameof(T.Prop) ]` ou array de strings.

---
## 8. Flattening (Projeção de Caminhos Aninhados)
O gerador tenta casar propriedades do DTO cujo nome é a concatenação sequencial (PascalCase) dos nomes das propriedades aninhadas no tipo origem.

Regras observadas (com base nos testes):
1. Cada segmento de nome deve corresponder exatamente a uma cadeia navegável de propriedades.  
2. O último segmento corresponde ao membro terminal (valor simples ou suportado).  
3. Nenhum atributo adicional é necessário; é puramente por convenção.  
4. Suporta profundidades múltiplas (ex.: `CustomerAddressCountryRegionName`).  
5. Colisões (dois caminhos possíveis para mesmo prefixo) devem ser evitadas; declare explicitamente propriedades intermediárias ou renomeie.  

Exemplos:

| Propriedade DTO | Caminho na origem |
|-----------------|-------------------|
| `AddressCity` | `Address.City` |
| `CustomerAddressCountryCode` | `Customer.Address.Country.Code` |
| `CustomerAddressCountryRegionName` | `Customer.Address.Country.Region.Name` |

Limitações atuais do flattening:
- Não há desambiguação quando múltiplos caminhos possíveis partilham prefixo idêntico; o comportamento depende da ordem de descoberta.
- Para alias explícito de origem, utilize `MapFrom`.

---
## 9. MapFrom (Alias explícito de origem)
`MapFrom` é a forma suportada de declarar alias/renome de mapeamento diretamente no DTO.

Quando usar:
- Preferir quando o nome do DTO não segue a convenção de flattening ou quando há colisões/ambiguidade.
- Útil para garantir clareza e evitar dependência da heurística de caminhos.

Boas práticas:
- Use `nameof(T.Prop)` sempre que possível para segurança em refactors.
- Evite caminhos inexistentes; o gerador emitirá diagnósticos em caso de propriedade inválida.

Interação com EF Core:
- O mapeamento gerado por `MapFrom` continua parte da `Expression` e é traduzível pelo provedor.

---
## 10. FAQ Rápido
**Flattening precisa de atributo?** Não, é por convenção do nome.  
**Qual profundidade máxima?** Não fixada; limitada apenas pela cadeia navegável e heurística de casamento.  
**Posso misturar flattening e objetos aninhados?** Sim.  
**E se dois caminhos produzirem mesmo prefixo?** Evite ou renomeie para clareza.  

---
Happy coding!
