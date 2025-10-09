# SmartSelector

Gerador de c�digo (Roslyn Source Generator) para cria��o de proje��es (DTOs) e propriedades autom�ticas fortemente tipadas, reduzindo boilerplate em consultas LINQ / EF Core e mapeamentos simples.

---
## Sum�rio
1. Vis�o Geral
2. Exemplos R�pidos (Quick Start)
3. Conceitos Principais
4. Atributos Dispon�veis
5. Membros Gerados
6. Propriedades Suportadas e Filtragem
7. Exclus�o de Propriedades
8. Flattening (Proje��o de Caminhos Aninhados)
9. FAQ R�pido

---
## 1. Vis�o Geral
O SmartSelector gera, a partir de classes decoradas com atributos, c�digo auxiliar para:
- Construir express�es de sele��o reutiliz�veis: `Expression<Func<TFrom, TModel>>`.
- Gerar m�todos de f�brica: `From(TFrom source)`.
- Gerar m�todos de extens�o para `IQueryable<TFrom>` e `IEnumerable<TFrom>`.
- Gerar automaticamente propriedades simples em DTOs a partir de um tipo de origem.
- Realizar flattening (achatar) de propriedades aninhadas por conven��o de nome.

---
## 2. Exemplos R�pidos (Quick Start)
A ideia �: voc� cria uma classe parcial vazia (ou quase) com atributos e o gerador cria o resto.

### 2.1 Proje��o simples + propriedades autom�ticas
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
C�digo gerado (essencial):
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

### 2.2 Proje��o com objeto aninhado e exclus�o
```csharp
[AutoSelect<Book>, AutoProperties(Exclude = [ nameof(Book.Sku) ])]
public partial class BookDetails
{
    public ShelfDetails Shelf { get; set; } // Navega��o explicitamente declarada
}

[AutoProperties<Shelf>]
public partial class ShelfDetails { }
```
Uso:
```csharp
var details = db.Books.Select(BookDetails.SelectBookExpression).ToList();
var single  = BookDetails.From(bookInstance);
```
C�digo gerado (resumo real):
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
        // Sku exclu�do
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

### 2.3 Somente propriedades autom�ticas
```csharp
[AutoProperties<Product>]
public partial class ProductSnapshot { }
```
C�digo gerado (exemplo):
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
Express�o gerada (trecho):
```csharp
AddressCity = a.Address.City
```

### 2.5 Flattening profundo (multi-n�vel)
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
Express�o gerada (trecho real testado):
```csharp
CustomerAddressCountryName = a.Customer.Address.Country.Name,
CustomerAddressCountryRegionName = a.Customer.Address.Country.Region.Name
```
A conven��o concatena os identificadores do caminho em PascalCase.

---
## 3. Conceitos Principais
| Conceito | Descri��o |
|----------|----------|
| `Model / Details` | Classe parcial alvo da proje��o (DTO). |
| `TFrom` | Tipo origem da proje��o. |
| `AutoSelect<TFrom>` | Ativa gera��o de express�o + extens�es. |
| `AutoProperties` / `AutoProperties<TFrom>` | Gera��o autom�tica de propriedades simples. |
| Flattening | Casamento por conven��o de nomes concatenados para acessar membros aninhados. |

---
## 4. Atributos Dispon�veis
Mesmos comportamentos j� descritos anteriormente (`AutoSelect<TFrom>`, `AutoProperties`, `AutoProperties<TFrom>`, `Exclude`).

---
## 5. Membros Gerados
| Membro | Descri��o |
|--------|----------|
| `Select{TFrom}Expression` | Express�o de proje��o. |
| `From` | Constr�i inst�ncia usando express�o compilada (cache). |
| `Select{Model}` (IQueryable/Enumerable) | M�todos de extens�o de proje��o. |
| `To{Model}` | Convers�o direta de inst�ncia �nica. |
| Propriedades auto | Adicionadas quando `AutoProperties` est� presente. |

---
## 6. Propriedades Suportadas e Filtragem
Mesmos crit�rios: primitivos, string, bool, DateTime, enum, struct e cole��es simples (`IEnumerable<T>` de tipo suportado).

---
## 7. Exclus�o de Propriedades
Via `Exclude = [ nameof(T.Prop) ]` ou array de strings.

---
## 8. Flattening (Proje��o de Caminhos Aninhados)
O gerador tenta casar propriedades do DTO cujo nome � a concatena��o sequencial (PascalCase) dos nomes das propriedades aninhadas no tipo origem.

Regras observadas (com base nos testes):
1. Cada segmento de nome deve corresponder exatamente a uma cadeia naveg�vel de propriedades.  
2. O �ltimo segmento corresponde ao membro terminal (valor simples ou suportado).  
3. Nenhum atributo adicional � necess�rio; � puramente por conven��o.  
4. Suporta profundidades m�ltiplas (ex.: `CustomerAddressCountryRegionName`).  
5. Colis�es (dois caminhos poss�veis para mesmo prefixo) devem ser evitadas; declare explicitamente propriedades intermedi�rias ou renomeie.  

Exemplos:
| Propriedade DTO | Caminho na origem |
|-----------------|-------------------|
| `AddressCity` | `Address.City` |
| `CustomerAddressCountryCode` | `Customer.Address.Country.Code` |
| `CustomerAddressCountryRegionName` | `Customer.Address.Country.Region.Name` |

Limita��es atuais do flattening:
- N�o h� desambigua��o quando m�ltiplos caminhos poss�veis partilham prefixo id�ntico; o comportamento depende da ordem de descoberta.
- N�o h� hoje suporte a mapeamento customizado (ex: abrevia��es, alias).

---
## 9. FAQ R�pido
**Flattening precisa de atributo?** N�o, � por conven��o do nome.  
**Qual profundidade m�xima?** N�o fixada; limitada apenas pela cadeia naveg�vel e heur�stica de casamento.  
**Posso misturar flattening e objetos aninhados?** Sim.  
**E se dois caminhos produzirem mesmo prefixo?** Evite ou renomeie para clareza.  

---
Happy coding!
