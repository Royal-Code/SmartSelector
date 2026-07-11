# Revisão técnica do SmartSelector

Data: 2026-07-10  
Escopo: problemas ainda não resolvidos após a revisão inicial e novos casos formalizados pela suíte TDD.

## Estado das ações da revisão inicial

Os seguintes itens foram tratados no código junto desta revisão:

- resolução semântica de `TFrom` em `AutoSelect<TFrom>`, incluindo nomes qualificados, `global::`, tipos aninhados e tipos genéricos construídos;
- igualdade e hash codes de `AutoPropertiesInformation`, `AutoSelectInformation` e `AutoDetailsInformation`;
- atualização de `AutoMapper` e EF Core SQLite para versões que removem as vulnerabilidades altas detectadas.

Os problemas abaixo continuam pendentes. A numeração foi refeita por prioridade e dependência técnica.

## 1. Semântica de null em projeções de objetos e coleções

### Problema

O generator produz acesso direto a todos os segmentos de uma navegação e chama `Select` diretamente sobre coleções. Por exemplo, uma origem `ICollection<Item>?` resulta em código equivalente a `source.Items.Select(...)`. Objetos intermediários nullable também são acessados diretamente.

Esse código possui duas formas de execução com semânticas diferentes:

- em um `IQueryable`, o provedor pode traduzir a árvore para SQL e aplicar sua própria semântica de null;
- no método `From`, a mesma expressão é compilada e executada como código .NET comum.

O comportamento que funciona em uma consulta de EF Core pode, portanto, lançar `NullReferenceException` quando usado por `From` ou pelas extensões de `IEnumerable<T>`.

### Consequências e implicações

- `From` não é semanticamente equivalente à projeção executada pelo provedor LINQ;
- entidades parcialmente carregadas ou grafos opcionais podem falhar em produção;
- a nulabilidade declarada no modelo de origem não é respeitada;
- uma correção ingênua com `?.` pode tornar a expression tree não traduzível, pois expression trees tradicionais não suportam todos os recursos modernos de C# e provedores variam no suporte a condicionais;
- decidir entre `null`, coleção vazia e DTO vazio é uma decisão de contrato da biblioteca, não apenas uma mudança sintática.

### Opções de solução

#### Opção A — Gerar condicionais explícitas na expression tree

Exemplo conceitual: `source.Items == null ? null : source.Items.Select(...).ToList()`.

Prós:

- mantém uma única expressão para `IQueryable` e `From`;
- preserva `null` quando o destino também é nullable;
- operadores condicionais costumam ser traduzidos pelos principais provedores.

Contras:

- aumenta bastante a complexidade para caminhos profundos;
- requer regras para combinações nullable → non-nullable;
- precisa ser validado contra cada versão/provedor de EF Core suportado.

#### Opção B — Usar coleção vazia ou DTO padrão como fallback

Exemplo conceitual: `source.Items == null ? [] : source.Items.Select(...).ToList()`.

Prós:

- facilita o consumo dos DTOs;
- evita propagar null para coleções;
- torna `From` mais previsível para APIs.

Contras:

- perde a distinção entre “não carregado/não informado” e “vazio”;
- pode contrariar a nulabilidade declarada no DTO;
- a criação de coleção vazia precisa ser expressável e traduzível.

#### Opção C — Manter o comportamento e diagnosticar grafos nullable

Prós:

- menor mudança no generator;
- deixa o risco explícito em tempo de compilação.

Contras:

- reduz muito a utilidade de `From`;
- transfere a responsabilidade para todo consumidor;
- não resolve a diferença entre execução em memória e pelo provedor.

### Recomendação

Adotar a opção A com uma política explícita:

- nullable → nullable: propagar `null`;
- nullable → non-nullable: emitir diagnóstico configurável, inicialmente warning;
- coleção nullable → coleção non-nullable: exigir uma opção explícita antes de adotar coleção vazia.

Começar por testes separados para execução em memória e EF Core SQLite. Isso evita declarar suporte baseado somente no formato do código gerado.

## 2. Contrato incompleto e nomenclatura rígida de `AutoDetails`

### Problema

`AutoDetails` usa o nome da propriedade de origem para localizar o membro, mas gera o tipo com a convenção `{SourceType}Details`, ignorando o tipo efetivamente declarado na propriedade de destino. Uma propriedade declarada como `AddressView Address` pode levar à geração de `AddressDetails`, deixando `AddressView` sem definição.

A alteração local que troca o segundo `Public()` por `Partial()` corrige a extensibilidade da classe gerada, mas não resolve a escolha do nome, colisões, tipos já existentes nem todas as combinações de acessibilidade.

### Consequências e implicações

- o contrato descrito pelo atributo não corresponde necessariamente ao código produzido;
- aliases de DTO ou convenções próprias de nome não funcionam;
- dois usos de `AutoDetails` para o mesmo tipo podem tentar gerar o mesmo arquivo/tipo;
- tipos internos e membros com tipos menos acessíveis podem gerar erros de acessibilidade;
- não está definido se um tipo já existente deve ser completado, reutilizado ou rejeitado.

### Opções de solução

#### Opção A — Gerar exatamente o tipo declarado na propriedade

Prós:

- segue o contrato expresso pelo código do usuário;
- permite nomes como `AddressView` e `AddressSummary`;
- reduz convenções implícitas.

Contras:

- exige distinguir error symbols de tipos já existentes;
- precisa validar se o tipo pode ser declarado naquele namespace/containing type;
- precisa definir o comportamento quando o tipo já existe e não é partial.

#### Opção B — Tornar o nome explícito no atributo

Exemplo conceitual: `[AutoDetails(Name = "AddressDetails")]`.

Prós:

- decisão inequívoca;
- simples de diagnosticar.

Contras:

- duplica informação que já existe no tipo da propriedade;
- nomes em string não acompanham refactors.

#### Opção C — Manter a convenção rígida e validar o tipo declarado

Prós:

- implementação menor;
- comportamento previsível para quem segue a convenção.

Contras:

- limita a API sem necessidade;
- exige documentação e diagnóstico específicos;
- mantém acoplamento forte ao sufixo `Details`.

### Recomendação

Adotar a opção A. O tipo da propriedade deve ser a fonte de verdade. Se o símbolo estiver ausente porque o tipo será gerado, usar sua sintaxe/nome declarado; se já existir, gerar uma parte apenas quando ele for `partial`. Emitir diagnósticos dedicados para tipo não parcial, acessibilidade incompatível e geração duplicada.

## 3. A suíte principal não valida a compilação final

### Problema

`Util.Compile` devolve separadamente o `Compilation` atualizado e os diagnósticos retornados por `RunGeneratorsAndUpdateCompilation`. A maioria dos testes verifica apenas o segundo conjunto. Um generator pode executar sem emitir RCSS e ainda gerar C# com erros `CSxxxx`.

A nova classe `GeneratedCodeCompilationTddTests` demonstra o problema sem alterar o helper antigo. Ela possui seu próprio conjunto completo de referências da plataforma e permanece deliberadamente vermelha.

### Consequências e implicações

- snapshots textuais podem aprovar código sintaticamente ou semanticamente inválido;
- mudanças no emitter podem quebrar consumidores sem quebrar os testes;
- warnings de nullable não fazem parte do critério de qualidade;
- o conjunto manual de referências do helper diverge do ambiente real do SDK;
- falhas envolvendo tipos genéricos e nesting ficam ocultas.

### Opções de solução

#### Opção A — Evoluir `Util.Compile` para devolver um resultado estruturado

O resultado conteria `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources` e `RunResult`.

Prós:

- torna impossível confundir diagnósticos do generator com os do compilador;
- centraliza referências, parse options e nullable context;
- facilita testes incrementais e snapshots.

Contras:

- exige migrar os testes existentes;
- pode produzir uma alteração extensa de uma vez.

#### Opção B — Manter a assinatura e adicionar `GetCompilationErrors`

Prós:

- mudança pequena;
- migração gradual.

Contras:

- mantém uma API fácil de usar incorretamente;
- testes antigos podem continuar sem validação.

#### Opção C — Usar uma biblioteca dedicada de testes de source generators

Prós:

- modela compilação, diagnósticos e arquivos gerados de maneira padronizada;
- reduz infraestrutura própria.

Contras:

- adiciona dependência e curva de aprendizado;
- snapshots atuais precisam ser adaptados.

### Recomendação

Adotar a opção A e fazer a validação de erros de compilação ser padrão. Separar uma asserção opcional de “zero warnings”, pois alguns testes de diagnóstico podem precisar compilar entrada propositalmente inválida. Usar `TRUSTED_PLATFORM_ASSEMBLIES` ou reference assemblies do TFM, evitando a lista manual atual.

## 4. DTOs de destino genéricos e aninhados não são preservados

### Problema

O generator recria a declaração de destino apenas com o nome simples. Parâmetros genéricos, constraints e a cadeia de tipos contenedores não são reproduzidos. Para um `EntityDetails<T>`, ele tende a gerar outra declaração `EntityDetails`; para `Container.EntityDetails`, tende a gerar um tipo no namespace, fora de `Container`.

### Consequências e implicações

- o código gerado não compila ou não completa o tipo anotado;
- constraints como `where T : class` são perdidas;
- acessibilidade e parâmetros genéricos dos containing types deixam de estar disponíveis;
- o erro aparece longe do atributo, dentro de um `.g.cs`.

### Opções de solução

#### Opção A — Gerar toda a cadeia de declarações parciais

Prós:

- suporte completo ao modelo de tipos de C#;
- mantém a API no local escolhido pelo consumidor.

Contras:

- exige emitir parâmetros, variance, constraints, tipo de declaração e modificadores de cada nível;
- aumenta a complexidade do `ClassGenerator` compartilhado.

#### Opção B — Rejeitar esses casos com diagnóstico

Prós:

- implementação rápida e falha clara;
- evita `.g.cs` inválido.

Contras:

- limita cenários legítimos;
- pode exigir breaking change futura ao adicionar suporte.

### Recomendação

No curto prazo, implementar a opção B com diagnósticos específicos. Em seguida, evoluir para a opção A começando por DTO genérico de nível superior e depois tipos aninhados. Não gerar silenciosamente uma declaração diferente da anotada.

## 5. `AutoProperties<T>` depende da forma sintática do atributo

### Problema

Embora o pipeline encontre o atributo pelo metadata name, o transform volta a procurar somente `IdentifierNameSyntax` e `GenericNameSyntax`. Formas válidas como `[global::RoyalCode.SmartSelector.AutoProperties<Entity>]` ou aliases podem ser ignoradas, resultando em nenhuma propriedade gerada.

### Consequências e implicações

- refactors que qualificam nomes mudam o comportamento do generator;
- o código compila até algum consumidor acessar a propriedade esperada;
- não há diagnóstico explicando que o atributo foi encontrado, mas não interpretado.

### Opções de solução

#### Opção A — Usar `context.Attributes` e `AttributeData`

Prós:

- independente da sintaxe;
- consistente com a correção feita para `AutoSelect<T>`;
- argumentos de tipo e named arguments já estão semanticamente resolvidos.

Contras:

- extração de `nameof` e localização precisa de diagnóstico pode exigir manter alguma ligação com a sintaxe.

#### Opção B — Ampliar o parser para todos os tipos de `NameSyntax`

Prós:

- preserva o desenho atual.

Contras:

- frágil e fácil de esquecer novas formas;
- duplica trabalho que Roslyn já realizou.

### Recomendação

Adotar a opção A. Usar símbolos para semântica e sintaxe apenas para localização de diagnósticos. Essa separação reduz falsos negativos e simplifica suporte a aliases e nomes qualificados.

## 6. O código gerado não é nullable-clean

### Problema

O cache do delegate é declarado como campo static não-nullable sem inicialização. Além disso, algumas atribuições e operações sobre coleções não respeitam as anotações nullable. Quando os snapshots gerados são compilados como fontes comuns com nullable habilitado, aparecem warnings como `CS8618`, `CS8601` e `CS8604`. Árvores adicionadas pelo generator que não carregam contexto nullable explícito também podem ficar em contexto oblivious, ocultando os mesmos riscos em vez de analisá-los.

### Consequências e implicações

- consumidores com `TreatWarningsAsErrors` não conseguem usar o pacote;
- warnings reais do projeto ficam misturados com ruído do generator;
- o warning do cache sugere um contrato falso: o campo começa necessariamente como null;
- desabilitar nullable no arquivo inteiro pode esconder erros reais no restante do código gerado.

### Opções de solução

#### Opção A — Modelar corretamente a nulabilidade

Exemplo: `private static Func<TFrom, TDto>? selectFunc;` e tipos de propriedades preservados pelo símbolo.

Prós:

- código honesto e compatível com `TreatWarningsAsErrors`;
- mantém análise ativa.

Contras:

- requer que `TypeDescriptor` preserve `NullableAnnotation`;
- expõe decisões pendentes do problema 1.

#### Opção B — Emitir `#nullable disable` no código gerado

Prós:

- remove warnings rapidamente;
- baixo esforço.

Contras:

- mascara problemas reais;
- piora a qualidade do contrato público gerado.

#### Opção C — Suprimir warnings específicos

Prós:

- melhor que desligar toda a análise;
- pode servir como transição.

Contras:

- ainda esconde defeitos se aplicado amplamente;
- exige manutenção conforme o código muda.

### Recomendação

Adotar a opção A. Corrigir primeiro o cache nullable, que é inequívoco, e tratar warnings de navegação junto com a política do problema 1. Usar supressão apenas quando houver justificativa local documentada.

## 7. Pipeline de CI e publicação não possui gates de qualidade

### Problema

O workflow é apenas manual, compila diretamente os dois projetos de pacote e publica todos os `.nupkg` encontrados. Ele não executa testes, auditoria, validação do pacote ou uma etapa protegida de release. Os nomes das etapas ainda fazem referência a SmartSearch.

### Consequências e implicações

- um pacote pode ser publicado mesmo com testes falhando;
- as novas suítes TDD deliberadamente vermelhas impediriam um gate simples até serem categorizadas ou filtradas;
- vulnerabilidades podem reaparecer sem detecção automática;
- não existe evidência de que o pacote funciona quando consumido como analyzer a partir do `.nupkg`;
- glob amplo aumenta o risco de publicar artefato inesperado.

### Opções de solução

#### Opção A — Separar CI e release

CI executa build/test/audit em PR e push. Release, acionado por tag ou manualmente, baixa artefatos já validados e publica com environment protegido.

Prós:

- separação clara de responsabilidades;
- pacote publicado é exatamente o testado;
- permite aprovação e secrets restritos.

Contras:

- workflow mais elaborado;
- exige estratégia de versões/tags.

#### Opção B — Manter workflow único com etapas sequenciais

Prós:

- simples de implantar.

Contras:

- mistura validação e mutação externa;
- reruns e aprovações são menos seguros.

### Recomendação

Adotar a opção A. Criar um teste de consumo que instala os dois `.nupkg` em um projeto temporário e compila uma projeção mínima. Enquanto os testes TDD forem intencionalmente vermelhos, marcá-los com trait explícita e excluí-los somente do gate temporário, nunca com `Skip`, para que a dívida permaneça visível.

## 8. README possui exemplo contraditório

### Problema

O exemplo de DTO aninhado combina `[AutoSelect<Order>]` com `[AutoProperties<Order>]`, enquanto o generator exige a forma não genérica quando `AutoSelect<T>` está presente.

### Consequências e implicações

- o usuário copia o quickstart avançado e recebe `RCSS003`;
- a documentação reduz a confiança nos diagnósticos e na API;
- README empacotado no NuGet replica o erro.

### Opções de solução

#### Opção A — Corrigir o exemplo para `[AutoProperties(...)]`

Prós:

- alinha imediatamente documentação e implementação;
- alteração mínima.

Contras:

- não evita regressão futura.

#### Opção B — Além da correção, transformar exemplos em testes compiláveis

Prós:

- documentação passa a ser verificada em CI;
- previne divergência de API.

Contras:

- exige ferramenta ou extração dos snippets.

### Recomendação

Adotar as duas opções em sequência: corrigir o exemplo e criar pelo menos um teste de compilação para cada snippet principal do README. Como esta tarefa pediu apenas documentação para os itens restantes, o README não foi alterado agora.

## Ordem recomendada de execução

1. Reestruturar o harness de testes e tornar os diagnósticos da compilação final visíveis.
2. Emitir diagnósticos temporários para DTOs genéricos/aninhados.
3. Tornar `AutoProperties<T>` semântico, como `AutoSelect<T>`.
4. Definir e implementar a política de null.
5. Tornar todo código gerado nullable-clean.
6. Completar o contrato de `AutoDetails`.
7. Corrigir e testar a documentação.
8. Implantar CI e release separados depois que a baseline de testes estiver controlada.
