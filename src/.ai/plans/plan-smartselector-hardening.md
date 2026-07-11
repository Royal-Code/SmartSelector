# Plan: Endurecimento e evolução do SmartSelector (`smartselector-hardening`)

## Status: RASCUNHO - aguardando respostas de Q1–Q5; nenhuma fase iniciada

## Progresso

`░░░░░░░░░░░░░░░` **0%** - 0 de 15 fases

| Fase | Estado |
|---|---|
| Fase 1 - Typos e documentação | Pendente |
| Fase 2 - Limpeza de Demo e Benchmarks | Pendente |
| Fase 3 - Empacotamento e dependências | Pendente |
| Fase 4 - Refactors internos do generator | Pendente |
| Fase 5 - Harness de testes com validação de compilação | Pendente |
| Fase 6 - CI e release com gates | Pendente |
| Fase 7 - Bugs de geração de baixo risco | Pendente |
| Fase 8 - Diagnósticos completos e localizados | Pendente |
| Fase 9 - AutoProperties<T> semântico | Pendente |
| Fase 10 - Contrato do AutoDetails | Pendente |
| Fase 11 - Código gerado auto-suficiente e nullable-clean | Pendente |
| Fase 12 - Política de null em From e coleções | Pendente |
| Fase 13 - DTOs genéricos e aninhados | Pendente |
| Fase 14 - Pipeline incremental sem retenção de símbolos | Pendente |
| Fase 15 - Features incrementais de mapeamento | Pendente |

> **Manutenção deste plano:** ao concluir as tarefas de uma fase, marque cada tarefa com `- [x]`,
> troque o **Estado** da fase para `Concluida` na tabela acima e atualize a barra de progresso
> (um bloco `█` por fase concluída, `%` e `X de N`). Exemplo de barra: `███░░░░░░░░░░░░`.
> Antes de fechar uma fase, confirme que decisões, critérios de aceite, testes e invariantes relacionados foram aplicados.

---

## Contexto

### Fontes verificadas

- `.ai/reviews/smart-selector-review-2026-07-10.md` — revisão técnica anterior com 8 problemas pendentes e recomendações (política de null, contrato AutoDetails, harness, DTOs genéricos/aninhados, AutoProperties sintático, nullable-clean, CI, README).
- `dotnet build SmartSelector.sln` (2026-07-11) — compila com 0 erros e 22 warnings (CS8618 no Demo/Library, CS0108 em `Tests/Models/Expected/BlogsPosts.cs`, CS8601/CS8604 em `Tests/Models/Expected/Nulls.cs`).
- `dotnet test RoyalCode.SmartSelector.Tests` (2026-07-11) — 33 aprovados, 3 reprovados (os 3 de `GeneratedCodeCompilationTddTests`, vermelhos por design).
- `dotnet test RoyalCode.SmartSelector.Demo` (2026-07-11) — 25/25 aprovados (EF Core + SQLite).
- Harness de casos-limite executado contra o generator compilado (2026-07-11) — confirmou os bugs B1, B2, B3, B4 e o silêncio de `[AutoProperties]` órfão; confirmou que `Exclude = null` não quebra.

### Estado atual do código (verificado em 2026-07-11)

- **B1 — colisão de hintName:** dois DTOs com mesmo nome em namespaces diferentes causam `ArgumentException` (hintName `Details.g.cs` duplicado) e CS8785; nenhum código é gerado. hintName definido só pelo nome da classe em `AutoSelectGenerator.Generate` e `AutoPropertiesGenerator.cs:362`.
- **B2 — AutoDetails com nome fora da convenção:** `AutoDetailsGenerator.cs:54` nomeia a classe gerada como `{TipoOrigem}Details`; propriedade declarada como `AddressDto` gera `AddressDetails` e a expressão referencia `AddressDto` inexistente → CS0246.
- **B3 — propriedade get-only duplicada:** filtro de exclusão em `AutoPropertiesGenerator.cs:182` usa `p.SetMethod is not null`; `public string Name => "x";` no DTO não é excluída → CS0102 no código gerado.
- **B4 — dependência de ImplicitUsings:** arquivos gerados não emitem `using System;`/`System.Linq`/`System.Collections.Generic`; compilação sem implicit usings falha com CS0246 para `Func<,>`, `IQueryable<>`, `IEnumerable<>`.
- **B5 — herança de DTOs:** `PostAndCommentsDetails : PostDetails` (ambos `AutoSelect<Post>`) gera membros que ocultam os herdados → CS0108 no consumidor.
- **Diagnósticos incorretos/silêncio:** classe não-partial com `AutoProperties<T>` reporta RCSS005 com mensagem de type argument; `[AutoProperties]` sem `AutoSelect` não gera nada nem diagnostica; `AutoDetailsGenerator.cs:33` cria diagnóstico com `location: null`; mensagem RCSS003 cita `AutoPropertyAttribute` (nome inexistente).
- **AutoProperties<T> sintático:** `AutoPropertiesGenerator.cs:54-57` filtra por `IdentifierNameSyntax`/`GenericNameSyntax`; forma qualificada (`[global::...AutoProperties<X>]`) é ignorada silenciosamente (teste TDD vermelho).
- **DTO genérico/aninhado:** geração produz declaração de nível de namespace com nome simples; 2 testes TDD vermelhos.
- **Retenção de símbolos no pipeline:** `TypeDescriptor` (pacote externo `RoyalCode.Extensions.SourceGenerator` 0.1.13) carrega `ISymbol`; os `*Information` carregam `Diagnostic[]` (com `Location`→`SyntaxTree`); mutações durante geração em `SelectLambdaGenerator.cs:109` (`AddParentProperty`) e `AutoDetailsGenerator.cs:47` (`Namespaces[0] = ...`).
- **Roslyn 5.6.0 no generator:** `RoyalCode.SmartSelector.Generators.csproj:21-22`; eleva o piso do consumidor para SDK .NET 10.0.3xx+, conflitando com o suporte declarado a net8.0/net9.0 da lib runtime.
- **README/docs divergentes:** `README.md:147` combina `AutoSelect<Order>` com `AutoProperties<Order>` (gera RCSS003); `README.md:8` tem seta corrompida (`?`); README/docs mostram `SelectXxxExpression =>` mas o código gerado usa `{ get; } =`.
- **CI:** workflow manual, sem execução de testes, sem gate, publica todo `.nupkg` por glob; nomes de etapas referenciam SmartSearch (fonte: revisão 2026-07-10, item 7).
- **Benchmarks:** `Generated_From` e `Generated_CachedDelegate` idênticos; descrição "AutoMapper ProjectTo List" usa `Map` por item; `var now` sem uso; `Console.WriteLine("Hello, World!")` em `Program.cs:4`.

### Lacunas, conflitos e restrições

- **Dependência do pacote externo `RoyalCode.Extensions.SourceGenerator`:** `TypeDescriptor`, `MatchSelection`, `ClassGenerator` etc. vivem fora deste repositório. Fases 10, 11, 13 e 14 podem exigir release coordenado do pacote externo (hoje 0.1.13).
- **Golden tests comparam texto integral:** qualquer mudança de emissão (usings, cabeçalho, XML docs) altera todos os snapshots de uma vez; regressões podem passar despercebidas em atualizações em massa.
- **Testes TDD vermelhos por design:** qualquer gate de CI precisa categorizá-los por trait antes de existir.

### Superfícies impactadas a mapear

- `RoyalCode.SmartSelector` (pacote runtime) — atributos públicos; mudanças são contrato público NuGet.
- `RoyalCode.SmartSelector.Generators` (pacote analyzer) — forma do código gerado é contrato de fato dos consumidores.
- `RoyalCode.Extensions.SourceGenerator` (repositório externo) — mudanças em `TypeDescriptor`/geradores exigem release próprio.
- `.github/workflows` — publicação NuGet.

---

## Objetivo

1. Zero bugs confirmados (B1–B5) reproduzíveis pelo harness de casos-limite.
2. Nenhum cenário não suportado falha em silêncio: todo caso rejeitado emite diagnóstico RCSS localizado.
3. Código gerado compila sem depender de `ImplicitUsings`, é nullable-clean e carrega cabeçalho auto-generated.
4. Suíte de testes valida a compilação final (erros CS) por padrão; os 3 testes TDD ficam verdes ou cobertos por diagnóstico.
5. `From`/extensões `IEnumerable` têm semântica de null definida, documentada e testada em memória e via EF Core SQLite.
6. Pipeline incremental sem retenção de `ISymbol`/`Diagnostic` nos modelos cacheados e sem mutação durante a geração.
7. CI executa build+testes como gate; release publica somente artefatos validados.
8. Documentação (README/docs.md) fiel ao código gerado e sem exemplos que produzem erro.

## Fora de escopo

- Mapeamento reverso (`ToEntity`/`Update`) — destino: backlog.
- Suporte a records, `init`/`required` members — destino: backlog.
- Interfaces como tipo de origem — destino: backlog.
- CodeFix providers para RCSS000/001 — destino: backlog.
- Renomear `{Dto}_Extensions` — destino: backlog (breaking; ver Q2).

---

## Perguntas ao humano

- **Q1 — Piso de Roslyn do generator:** manter `Microsoft.CodeAnalysis.CSharp` 5.6.0 ou reduzir para ampliar compatibilidade?
  - **Opções:**
    - **A)** Reduzir para 4.8.0 — consumidores com SDK .NET 8.0.1xx+ conseguem usar o generator; recomendado, o código não usa API nova de Roslyn.
    - **B)** Manter 5.6.0 — exige SDK .NET 10.0.3xx+; consumidores net8/net9 ficam sem o generator.
  - **Impacto se não decidir:** Fase 3 bloqueada; pacote publicado pode ser inutilizável para parte do público declarado.
  - **Status:** Aberta.

- **Q2 — Mudanças breaking na API pública dos atributos:** selar os atributos, extrair classe base com `Exclude`/`Flattening` e/ou renomear `{Dto}_Extensions` para `{Dto}Extensions`?
  - **Opções:**
    - **A)** Aplicar tudo agora (projeto em 0.x, breaking aceitável).
    - **B)** Aplicar apenas `sealed` (menor risco) e diferir o resto.
    - **C)** Não mudar contrato público.
  - **Impacto se não decidir:** tarefas correspondentes da Fase 4 ficam bloqueadas; o restante da fase não depende disso.
  - **Status:** Aberta.

- **Q3 — Fallback para coleção de origem nullable projetada em coleção de destino non-nullable:** a revisão 2026-07-10 recomenda propagar `null` quando destino é nullable, mas exige decisão explícita para destino non-nullable.
  - **Opções:**
    - **A)** Coleção vazia (`... == null ? new List<T>() : ...`) — consumo previsível, perde distinção null/vazio.
    - **B)** Propagar `null` mesmo em destino non-nullable + diagnóstico warning — honesto, mas viola anotação do DTO.
    - **C)** Diagnóstico de erro exigindo destino nullable — mais restritivo.
  - **Impacto se não decidir:** Fase 12 bloqueada.
  - **Status:** Aberta.

- **Q4 — Estratégia de release no CI:** disparo por tag `v*` com environment protegido, ou manual (`workflow_dispatch`) com aprovação?
  - **Opções:**
    - **A)** Tag + environment protegido — release rastreável pelo git.
    - **B)** Manual com aprovação — fluxo atual preservado, com gate adicionado.
  - **Impacto se não decidir:** Fase 6 implementa apenas o CI de validação; job de release fica pendente.
  - **Status:** Aberta.

- **Q5 — DTOs genéricos:** após suportar DTOs aninhados (viáveis), suportar também DTO genérico (`EntityDetails<T>`) ou rejeitar com diagnóstico permanente?
  - **Opções:**
    - **A)** Suportar (emitir type parameters e constraints) — custo alto no `ClassGenerator` externo.
    - **B)** Diagnóstico permanente + teste TDD convertido em teste de diagnóstico.
  - **Impacto se não decidir:** Fase 13 executa somente a parte de tipos aninhados.
  - **Status:** Aberta.

---

## Decisões fechadas

- **DF1 — hintName qualificado:** hintName dos arquivos gerados passa a incluir o namespace (ex.: `Probe1.Details.g.cs`). Fonte: bug B1 confirmado por harness em 2026-07-11.
- **DF2 — AutoDetails gera o tipo declarado na propriedade:** o nome/namespace da classe gerada vem de `property.Type`, não de `{TipoOrigem}Details`. Fonte: revisão 2026-07-10, item 2, opção A.
- **DF3 — AutoProperties<T> resolvido semanticamente:** usar `context.Attributes`/`AttributeData` para tipo e named arguments; sintaxe apenas para localização de diagnóstico. Fonte: revisão 2026-07-10, item 5, opção A.
- **DF4 — Harness devolve resultado estruturado:** `Util.Compile` evolui para resultado com `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources`; validação de erros de compilação vira padrão. Fonte: revisão 2026-07-10, item 3, opção A.
- **DF5 — Política de null direcional:** nullable → nullable propaga `null` via condicional na expression tree; nullable → non-nullable escalar emite diagnóstico warning. Fonte: revisão 2026-07-10, item 1, opção A (fallback de coleção pendente em Q3).
- **DF6 — Nullable-clean por modelagem:** campo de cache vira `Func<TFrom, TDto>?`; sem `#nullable disable` global no código gerado. Fonte: revisão 2026-07-10, item 6, opção A.
- **DF7 — DTOs genéricos/aninhados: diagnóstico primeiro, suporte depois:** Fase 8 emite diagnósticos temporários; Fase 13 implementa suporte a aninhados. Fonte: revisão 2026-07-10, item 4.
- **DF8 — Idioma:** comentários de código em português; identificadores, diagnósticos e XML docs públicos em inglês. Fonte: predominância verificada no código atual.
- **DF9 — Testes TDD nunca recebem `Skip`:** categorização por trait explícita (ex.: `Category=KnownLimitation`) e filtro no gate de CI. Fonte: revisão 2026-07-10, item 7.
- **DF10 — README corrigido e verificado:** corrigir exemplo contraditório e criar teste de compilação para os snippets principais. Fonte: revisão 2026-07-10, item 8, opções A+B.
- **DF11 — Cabeçalho e metadados de geração:** arquivos gerados recebem `// <auto-generated/>` e membros públicos recebem XML docs; classes geradas recebem `[GeneratedCode]`. Fonte: análise 2026-07-11 (CS1591 em consumidores com docs obrigatórias).

---

## Histórico de decisões

> Sem perguntas respondidas até o momento. Registrar aqui quando Q1–Q5 forem respondidas.

---

## Design alvo

### Contratos e bordas

- `RoyalCode.SmartSelector` (atributos): `AutoSelectAttribute<TFrom>`, `AutoPropertiesAttribute(<TFrom>)`, `AutoDetailsAttribute`, `MapFromAttribute` — assinatura estável salvo decisão em Q2.
- Código gerado por DTO: `Select{TFrom}Expression` (property com initializer), `From(TFrom)`, extensões `Select{Dto}` para `IQueryable<TFrom>`/`IEnumerable<TFrom>`, `To{Dto}` — nomes preservados; conteúdo passa a ser auto-suficiente (usings), nullable-clean e com cabeçalho auto-generated.
- Diagnósticos: RCSS000–005 mantidos; novos IDs (RCSS006+) para: classe não-partial em AutoProperties, `[AutoProperties]` órfão, DTO aninhado/genérico não suportado, colisão de flattening, nullable → non-nullable. Todos com `Location` apontando o símbolo do usuário e registrados em `AnalyzerReleases.Unshipped.md`.

### Modelo, dados e persistência

```text
Modelos do pipeline incremental (alvo da Fase 14)
  *Information            somente dados equatable (strings, arrays, structs)
  DiagnosticInfo          id + location serializável (FilePath, TextSpan, LineSpan)
  sem ISymbol             símbolos consumidos e descartados no Transform
```

### Arquitetura alvo

```text
RoyalCode.SmartSelector/
  atributos públicos (runtime, sem lógica)

RoyalCode.SmartSelector.Generators/
  Transform: resolução 100% semântica -> modelos puros equatable
  Generate: função pura do modelo (sem mutação de estado)
  emissão: usings completos, auto-generated header, XML docs

RoyalCode.SmartSelector.Tests/
  Util -> CompileResult estruturado (GeneratorDiagnostics, CompilationDiagnostics, GeneratedSources)
  golden tests + testes de diagnóstico + traits para limitações conhecidas

.github/workflows/
  ci.yml: build + test (filtro KnownLimitation) + pack em PR/push
  release.yml: publica artefatos do CI validado
```

### Segurança, concorrência e confiabilidade

- Expressões geradas permanecem traduzíveis por EF Core; toda mudança de emissão passa pelos testes do Demo (SQLite).
- Cache do delegate `From` permanece por tipo, estático, com atribuição idempotente (`??=`).
- Release publica somente artefatos produzidos por run de CI verde (após Fase 6).

### Compatibilidade, migração e rollout

- Piso de Roslyn do generator definido por Q1; documentar SDK mínimo no README.
- Mudanças na forma do código gerado (Fases 11–12) são observáveis por consumidores: registrar no changelog/release notes.
- Breaking de API pública somente com Q2 respondida; versão 0.x permite, mas exige nota de release.

---

## Ordem de execução

1. **Fase 1 (Typos e documentação)** — sem dependências; risco zero.
2. **Fase 2 (Limpeza Demo/Benchmarks)** — sem dependências; reduz ruído de warnings.
3. **Fase 3 (Empacotamento e dependências)** — depende de Q1.
4. **Fase 4 (Refactors internos)** — antes das correções de comportamento para reduzir duplicação a manter.
5. **Fase 5 (Harness de testes)** — pré-requisito técnico das fases 7–13.
6. **Fase 6 (CI e release)** — depende dos traits da Fase 5; protege as fases seguintes.
7. **Fase 7 (Bugs de baixo risco)** — correções pontuais com harness pronto.
8. **Fase 8 (Diagnósticos)** — elimina falhas silenciosas antes de mudanças maiores.
9. **Fase 9 (AutoProperties semântico)** — zera 1 teste TDD.
10. **Fase 10 (Contrato AutoDetails)** — depende de DF2 e Fase 8.
11. **Fase 11 (Código gerado auto-suficiente/nullable-clean)** — mexe em todos os snapshots; exige Fases 5 e 7 concluídas.
12. **Fase 12 (Política de null)** — depende de Q3 e Fase 11.
13. **Fase 13 (Genéricos e aninhados)** — depende de Q5 e Fase 8.
14. **Fase 14 (Pipeline sem símbolos)** — refactor mais profundo; pode exigir release do pacote externo.
15. **Fase 15 (Features de mapeamento)** — sobre base estabilizada.

Build/test padrão:

```powershell
dotnet build SmartSelector.sln
dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj
dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj
```

---

## Fase 1 - Typos e documentação

**Depende de:** DF10.

**Escopo:** `README.md`, `docs.md`, `AnalyzerDiagnostics.cs`, `AutoSelectGenerator.cs`, `AutoPropertiesGenerator.cs`, `MapFromAttribute.cs`, `InternalVisible.cs`.

**O que/como:** correções de texto sem mudança de comportamento; a única mudança observável é a string da mensagem RCSS003.

**Tarefas:**

- [ ] Corrigir "extenção" → "extensão" em `AutoSelectGenerator.cs:262`.
- [ ] Corrigir mensagem RCSS003 em `AnalyzerDiagnostics.cs:36`: `AutoPropertyAttribute` → `AutoPropertiesAttribute` (duas ocorrências).
- [ ] Corrigir "processa se propriedade se tem atributo" em `AutoPropertiesGenerator.cs:186`.
- [ ] Corrigir seta corrompida (`?`) em `README.md:8`.
- [ ] Corrigir exemplo contraditório `[AutoSelect<Order>, AutoProperties<Order>(...)]` em `README.md:147` para a forma não genérica.
- [ ] Atualizar README/docs.md para mostrar `{ get; } =` no lugar de `=>` em `SelectXxxExpression` (README:62-68; docs.md:48, docs.md:93).
- [ ] Corrigir recuo do item `10.` no sumário de `docs.md:16`.
- [ ] Remover `#pragma warning disable CS9113` obsoleto de `MapFromAttribute.cs:3`.
- [ ] Remover linha em branco inicial de `InternalVisible.cs`.

**Critérios de aceite:** `grep` não encontra "extenção", "AutoPropertyAttribute" nem `CS9113` no repositório; README não contém `AutoProperties<Order>` junto de `AutoSelect<Order>`; build e testes inalterados (33/36 + 25/25).

**Testes:** `dotnet build SmartSelector.sln`; `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj`.

### Resultado da Fase 1

*a preencher*

---

## Fase 2 - Limpeza de Demo e Benchmarks

**Depende de:** nada.

**Escopo:** `RoyalCode.SmartSelector.Demo/Entities/Library/*`, `Details/Library/BookDetails.cs`, `RoyalCode.SmartSelector.Benchmarks/*`.

**O que/como:** eliminar warnings e ruído dos projetos auxiliares sem alterar cenários testados.

**Tarefas:**

- [ ] Adicionar `#nullable disable` (padrão dos demais arquivos do Demo) ou inicializadores em `Book.cs`, `Shelf.cs` e `BookDetails.cs` para zerar os 6 CS8618 do Demo.
- [ ] Remover `Console.WriteLine("Hello, World!")` de `Benchmarks/Program.cs:4`.
- [ ] Remover variável `now` sem uso em `ProductMappingBenchmark.cs:22`.
- [ ] Remover ou diferenciar o benchmark duplicado `Generated_CachedDelegate` (idêntico a `Generated_From`).
- [ ] Corrigir descrição "AutoMapper ProjectTo List" para refletir `Map` por item (ou trocar a implementação para `ProjectTo`).

**Critérios de aceite:** `dotnet build` do Demo sem nenhum CS8618; benchmarks compilam; nenhuma dupla de benchmarks com corpo idêntico.

**Testes:** `dotnet build SmartSelector.sln`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`; `dotnet build RoyalCode.SmartSelector.Benchmarks\RoyalCode.SmartSelector.Benchmarks.csproj -c Release`.

### Resultado da Fase 2

*a preencher*

---

## Fase 3 - Empacotamento e dependências

**Depende de:** Q1.

**Escopo:** `RoyalCode.SmartSelector.Generators.csproj`, `RoyalCode.SmartSelector.Demo.csproj`, `RoyalCode.SmartSelector.Benchmarks.csproj`, `tests.targets`, `README.md`.

**O que/como:** fixar o piso de Roslyn decidido em Q1 e remover caminhos hardcoded de empacotamento.

**Tarefas:**

- [ ] Aplicar a versão de `Microsoft.CodeAnalysis.*` decidida em Q1 no projeto do generator (testes podem permanecer em versão mais nova).
- [ ] Trocar `$(NuGetPackageRoot)royalcode.extensions.sourcegenerator\...` por `GeneratePathProperty="true"` + `$(PKGRoyalCode_Extensions_SourceGenerator)` nos 3 csproj que usam o caminho.
- [ ] Reavaliar `NoWarn` de `NU1900` no generator; remover se o restore não o exigir mais.
- [ ] Documentar no README o SDK mínimo exigido pelo generator.

**Critérios de aceite:** `dotnet pack` do generator produz `.nupkg` contendo `RoyalCode.SmartSelector.Generators.dll` e `RoyalCode.Extensions.SourceGenerator.dll` em `analyzers/dotnet/cs`; build limpo após remoção do caminho hardcoded.

**Testes:** `dotnet pack RoyalCode.SmartSelector.Generators\RoyalCode.SmartSelector.Generators.csproj -c Release`; inspecionar o `.nupkg` gerado; suítes completas verdes.

### Resultado da Fase 3

*a preencher*

---

## Fase 4 - Refactors internos do generator

**Depende de:** Q2 (somente para as tarefas de API pública; demais tarefas livres).

**Escopo:** `RoyalCode.SmartSelector.Generators/**`, `RoyalCode.SmartSelector/*.cs`.

**O que/como:** eliminar duplicação sem alterar o texto gerado (golden tests devem passar sem edição).

**Tarefas:**

- [ ] Extrair helper único para mapear `DeclaredAccessibility` → modificadores (hoje triplicado em `AutoSelectGenerator`, `AutoPropertiesGenerator`, `AutoDetailsGenerator`), com fallback definido e tratamento de `ProtectedOrInternal`/`ProtectedAndInternal`.
- [ ] Extrair `SequenceEqual`/`SequenceHashCode` duplicados das 3 classes `*Information` para utilitário interno.
- [ ] Consolidar os 4 overloads de `CreateInformation` em `AutoPropertiesGenerator`.
- [ ] Unificar os `Predicate` idênticos de `AutoSelectGenerator` e `AutoPropertiesGenerator`.
- [ ] Padronizar idioma dos comentários conforme DF8.
- [ ] (Q2) Selar atributos e/ou extrair classe base `Exclude`/`Flattening` conforme decisão.

**Critérios de aceite:** todos os golden tests passam sem nenhuma alteração de string esperada; nenhuma duplicação das três estruturas citadas (verificável por grep).

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 4

*a preencher*

---

## Fase 5 - Harness de testes com validação de compilação

**Depende de:** DF4, DF9.

**Escopo:** `RoyalCode.SmartSelector.Tests/Util.cs`, todos os arquivos de teste, `GeneratedCodeCompilationTddTests.cs`.

**O que/como:** implementar o resultado estruturado (revisão item 3, opção A) e migrar os testes existentes.

**Tarefas:**

- [ ] Criar `CompileResult` com `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources` (por hintName) e `RunResult`.
- [ ] Usar `TRUSTED_PLATFORM_ASSEMBLIES` (como já faz `GeneratedCodeCompilationTddTests`) no lugar da lista manual de referências.
- [ ] Migrar todos os testes para o novo helper, mantendo as asserções de snapshot por hintName em vez de índice de árvore (`SyntaxTrees.Skip(n)`).
- [ ] Tornar padrão a asserção "zero erros de compilação final"; asserção opcional de "zero warnings".
- [ ] Marcar os 3 testes TDD com trait `Category=KnownLimitation` (sem `Skip`).

**Critérios de aceite:** nenhum teste acessa `SyntaxTrees.Skip(n)`; todo teste de geração falha se o código gerado tiver erro CS; `dotnet test --filter "Category!=KnownLimitation"` fica 100% verde.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`.

### Resultado da Fase 5

*a preencher*

---

## Fase 6 - CI e release com gates

**Depende de:** Fase 5 (traits), Q4.

**Escopo:** `.github/workflows/*`.

**O que/como:** separar CI de release (revisão item 7, opção A) e corrigir nomes herdados de SmartSearch.

**Tarefas:**

- [ ] Criar/ajustar workflow de CI: build + `dotnet test --filter "Category!=KnownLimitation"` + `dotnet pack` em PR e push para main.
- [ ] Criar teste de consumo: instalar os `.nupkg` produzidos em projeto temporário e compilar uma projeção mínima.
- [ ] Criar workflow de release conforme Q4, publicando somente artefatos do CI validado (sem glob amplo).
- [ ] Corrigir nomes de etapas que referenciam SmartSearch.

**Critérios de aceite:** PR com teste falhando bloqueia merge; release não executa sem CI verde; teste de consumo compila usando exclusivamente os `.nupkg`.

**Testes:** execução dos workflows em PR de teste; `gh run watch` verde.

### Resultado da Fase 6

*a preencher*

---

## Fase 7 - Bugs de geração de baixo risco

**Depende de:** DF1, Fase 5.

**Escopo:** `AutoSelectGenerator.cs`, `AutoPropertiesGenerator.cs`, geradores de emissão; golden tests.

**O que/como:** corrigir B1, B3, B4 e B5 com um teste novo por bug (usar os cenários do harness de 2026-07-11 como base).

**Tarefas:**

- [ ] B1: incluir namespace no hintName de todos os arquivos gerados (`{Namespace}.{Classe}.g.cs`, `{Namespace}.{Classe}.AutoProperties.g.cs`, `{Namespace}.{Classe}.AutoDetails.g.cs`); teste com dois DTOs homônimos em namespaces distintos.
- [ ] B3: ampliar exclusão de propriedades declaradas para qualquer propriedade declarada no DTO (remover condição `SetMethod is not null` da exclusão); teste com propriedade get-only homônima.
- [ ] B4: emitir `using System;`, `using System.Linq;`, `using System.Collections.Generic;` (ou nomes `global::`-qualificados) em todos os arquivos gerados; teste compilando sem implicit usings.
- [ ] B5: emitir modificador `new` em `Select{TFrom}Expression`, `From` e campo quando a base do DTO já declara membro homônimo; teste com DTO herdando DTO.

**Critérios de aceite:** os 4 cenários compilam sem CS8785/CS0102/CS0246/CS0108; snapshots existentes atualizados de forma revisável (diff contém apenas usings/`new` esperados).

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 7

*a preencher*

---

## Fase 8 - Diagnósticos completos e localizados

**Depende de:** DF7, Fase 5.

**Escopo:** `AnalyzerDiagnostics.cs`, `AutoPropertiesGenerator.cs`, `AutoDetailsGenerator.cs`, `AnalyzerReleases.Unshipped.md`.

**O que/como:** eliminar falhas silenciosas e mensagens incorretas; novos IDs a partir de RCSS006.

**Tarefas:**

- [ ] Criar diagnóstico específico para "classe com AutoProperties<T> não é partial" (substituir uso indevido de RCSS005 em `AutoPropertiesGenerator.cs:45-51`).
- [ ] Criar diagnóstico para `[AutoProperties]` sem `[AutoSelect<T>]` (hoje silencioso).
- [ ] Criar diagnósticos temporários para DTO genérico e DTO aninhado (removidos/reduzidos na Fase 13).
- [ ] Adicionar `Location` real ao diagnóstico de `AutoDetailsGenerator.cs:33` (apontar a propriedade).
- [ ] Criar diagnóstico warning para ambiguidade de flattening (múltiplos caminhos com mesmo prefixo).
- [ ] Registrar todos os novos IDs em `AnalyzerReleases.Unshipped.md`.
- [ ] Criar um teste de diagnóstico por ID novo (asserção de ID + location).

**Critérios de aceite:** todo cenário rejeitado emite ao menos um RCSS com location válida; nenhum caso do harness gera "0 diagnósticos + 0 código gerado".

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`.

### Resultado da Fase 8

*a preencher*

---

## Fase 9 - AutoProperties<T> semântico

**Depende de:** DF3, Fase 5.

**Escopo:** `AutoPropertiesGenerator.Transform`, `MapFromPropertyNameResolver` (avaliar remoção do fallback sintático).

**O que/como:** resolver TFrom, `Exclude` e `Flattening` via `context.Attributes`/`AttributeData`; sintaxe apenas para location de diagnóstico.

**Tarefas:**

- [ ] Reescrever `Transform` para usar `context.Attributes[0]` (TFrom via `AttributeClass.TypeArguments`, named args via `NamedArguments`).
- [ ] Remover a busca sintática por `IdentifierNameSyntax`/`GenericNameSyntax` (`AutoPropertiesGenerator.cs:54-57`), mantendo detecção de conflito genérico/não-genérico por símbolo.
- [ ] Avaliar e, se possível, remover o fallback de inspeção sintática de `MapFromPropertyNameResolver` (código morto se `ConstructorArguments` sempre materializa).
- [ ] Tornar verde o teste TDD `Generated_code_should_compile_with_a_fully_qualified_AutoProperties_attribute` e remover seu trait `KnownLimitation`.

**Critérios de aceite:** `[global::RoyalCode.SmartSelector.AutoProperties<X>]` e aliases geram o mesmo código da forma simples; teste TDD correspondente verde e no gate padrão.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj` (2 vermelhos restantes no máximo, ambos `KnownLimitation`).

### Resultado da Fase 9

*a preencher*

---

## Fase 10 - Contrato do AutoDetails

**Depende de:** DF2, Fase 8.

**Escopo:** `AutoDetailsGenerator.cs`, testes de AutoDetails.

**O que/como:** o tipo declarado na propriedade é a fonte de verdade (revisão item 2, opção A).

**Tarefas:**

- [ ] Usar nome/namespace de `property.Type` para a classe gerada (corrige B2).
- [ ] Tratar tipo já existente: gerar parte apenas se `partial`; diagnóstico caso contrário.
- [ ] Diagnóstico para geração duplicada (duas propriedades `[AutoDetails]` para o mesmo tipo) e para acessibilidade incompatível.
- [ ] Remover a mutação `propertyType.Namespaces[0] = ...` (`AutoDetailsGenerator.cs:47`) construindo descriptor novo.
- [ ] Testes: tipo com nome fora da convenção (`AddressDto`), tipo pré-existente partial, duplicidade.

**Critérios de aceite:** cenário `AddressDto` do harness compila; nenhum caso de AutoDetails gera classe com nome diferente do declarado.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 10

*a preencher*

---

## Fase 11 - Código gerado auto-suficiente e nullable-clean

**Depende de:** DF6, DF11, Fases 5 e 7.

**Escopo:** emissores (`ClassGenerator` uso, `AutoSelectGenerator`, `AutoPropertiesGenerator`, `AutoDetailsGenerator`); todos os snapshots; possivelmente `RoyalCode.Extensions.SourceGenerator`.

**O que/como:** cabeçalho, metadados e nulabilidade correta no código emitido.

**Tarefas:**

- [ ] Emitir `// <auto-generated/>` no topo de todo arquivo gerado.
- [ ] Emitir `[GeneratedCode("RoyalCode.SmartSelector.Generators", "<versão>")]` nas classes geradas.
- [ ] Emitir XML docs (`/// <summary>`) nos membros públicos gerados (elimina CS1591 em consumidores com docs obrigatórias).
- [ ] Declarar o cache como `private static Func<TFrom, TDto>? select{X}Func;`.
- [ ] Preservar anotações nullable nos tipos das propriedades geradas (exige `NullableAnnotation` no `TypeDescriptor` — coordenar com pacote externo se necessário).
- [ ] Atualizar todos os golden tests em commit separado das mudanças de emissor (diff revisável).

**Critérios de aceite:** snapshots compilados como fonte comum com `nullable enable` não emitem CS8618 pelo campo de cache; arquivos gerados começam com `// <auto-generated/>`; consumidor com `GenerateDocumentationFile=true` não recebe CS1591 de membros gerados.

**Testes:** suítes completas + verificação manual de um `.g.cs` do Demo (`EmitCompilerGeneratedFiles`).

### Resultado da Fase 11

*a preencher*

---

## Fase 12 - Política de null em From e coleções

**Depende de:** DF5, Q3, Fase 11.

**Escopo:** `SelectLambdaGenerator.cs`, `MatchSelection`/`AssignDescriptor` (pacote externo, se necessário), testes novos de execução em memória e EF Core.

**O que/como:** implementar a opção A da revisão (condicionais explícitas na expression tree) com a política direcional de DF5 e o fallback decidido em Q3.

**Tarefas:**

- [ ] Navegação nullable → destino nullable: gerar `a.X == null ? null : ...` (objetos e flattening).
- [ ] Escalar nullable → destino non-nullable: manter comportamento atual + diagnóstico warning (DF5).
- [ ] Coleção nullable: aplicar decisão de Q3.
- [ ] Testes de execução em memória (`From`, `Select` de `IEnumerable`) com grafos nulos — sem `NullReferenceException` nos casos cobertos pela política.
- [ ] Testes EF Core SQLite no Demo garantindo tradutibilidade das novas condicionais.
- [ ] Documentar a política em `docs.md` (nova seção).

**Critérios de aceite:** `From` com navegação nula não lança NRE quando destino é nullable; consultas do Demo continuam traduzindo (25/25 + novos casos verdes); política documentada.

**Testes:** `dotnet test` das duas suítes; novos testes nomeados `NullPolicy*`.

### Resultado da Fase 12

*a preencher*

---

## Fase 13 - DTOs genéricos e aninhados

**Depende de:** DF7, Q5, Fase 8.

**Escopo:** emissão de declarações parciais (`ClassGenerator` — provável mudança no pacote externo), `AutoSelectGenerator`, `AutoPropertiesGenerator`.

**O que/como:** gerar a cadeia completa de declarações contendo (revisão item 4, opção A), começando por tipos aninhados; genéricos conforme Q5.

**Tarefas:**

- [ ] Emitir cadeia de tipos contenedores (`partial class Container { partial class EntityDetails { ... } }`) com modificadores corretos.
- [ ] Tornar verde `Generated_code_should_compile_for_a_nested_destination_dto` e remover trait.
- [ ] (Q5=A) Emitir type parameters/constraints do DTO genérico e tornar verde o teste TDD restante; (Q5=B) converter o teste em teste de diagnóstico permanente.
- [ ] Remover os diagnósticos temporários da Fase 8 para os cenários que passaram a ser suportados.

**Critérios de aceite:** zero testes com trait `KnownLimitation` restantes (ou somente o de genérico, convertido em diagnóstico, conforme Q5).

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj` (sem filtro) verde.

### Resultado da Fase 13

*a preencher*

---

## Fase 14 - Pipeline incremental sem retenção de símbolos

**Depende de:** Fases 7–13 (evitar refazer trabalho sobre modelos que ainda vão mudar).

**Escopo:** `AutoSelectInformation`, `AutoPropertiesInformation`, `AutoDetailsInformation`, `Transform`s, `SelectLambdaGenerator`; `RoyalCode.Extensions.SourceGenerator` (release coordenado provável).

**O que/como:** extrair no Transform todos os dados necessários para strings/estruturas equatable; `Diagnostic` real criado apenas em `RegisterSourceOutput` a partir de `DiagnosticInfo` serializável; geração 100% pura.

**Tarefas:**

- [ ] Definir `DiagnosticInfo` (id, args, FilePath+TextSpan+LineSpan) e substituir `Diagnostic[]` nos modelos.
- [ ] Remover `ISymbol` dos dados retidos pelos modelos (consumir `TypeDescriptor.Symbol` dentro do Transform e descartar).
- [ ] Eliminar mutações durante a geração (`AddParentProperty` em `SelectLambdaGenerator.cs:109` → cálculo de caminho imutável).
- [ ] Adicionar teste de cacheabilidade usando `GeneratorDriver` com `trackIncrementalGeneratorSteps: true` verificando `IncrementalStepRunReason.Cached`/`Unchanged` após edição irrelevante.
- [ ] Coordenar release do `RoyalCode.Extensions.SourceGenerator` se `TypeDescriptor`/`MatchSelection` precisarem de forma sem símbolos.

**Critérios de aceite:** teste de cacheabilidade passa (edição em arquivo não relacionado não reexecuta o output); golden tests inalterados; nenhuma referência a `ISymbol` nos campos dos `*Information`.

**Testes:** suítes completas + teste de steps incrementais novo.

### Resultado da Fase 14

*a preencher*

---

## Fase 15 - Features incrementais de mapeamento

**Depende de:** Fases 11–14 (base estabilizada).

**Escopo:** atributos públicos, generators, docs, testes.

**O que/como:** features pequenas aprovadas nesta análise; cada uma com testes de geração + execução + EF quando aplicável.

**Tarefas:**

- [ ] Adicionar `Exclude` (e `Flattening`) diretamente em `AutoSelectAttribute<TFrom>` sem exigir `AutoProperties`.
- [ ] Suportar arrays (`T[]`) como propriedades simples em `IsSupportedType` e nas atribuições de coleção.
- [ ] Suportar caminho aninhado em `MapFrom` (`[MapFrom("Address.City")]`) com diagnóstico para caminho inválido.
- [ ] Documentar as três features em `docs.md` e README.

**Critérios de aceite:** cada feature tem teste de geração, teste de execução em memória e (quando envolver query) teste EF no Demo; docs atualizadas.

**Testes:** suítes completas verdes.

### Resultado da Fase 15

*a preencher*

---

## Matriz de rastreabilidade

| Objetivo | Fase(s) | Decisão(es) | Critério(s) de aceite | Teste(s) |
|---|---|---|---|---|
| 1. Zero bugs B1–B5 | 7, 10 | DF1, DF2 | cenários do harness compilam | testes novos das Fases 7 e 10 |
| 2. Sem falhas silenciosas | 8, 9 | DF3, DF7 | todo caso rejeitado emite RCSS com location | testes de diagnóstico da Fase 8 |
| 3. Código gerado auto-suficiente/limpo | 7 (B4), 11 | DF6, DF11 | compila sem ImplicitUsings; sem CS8618/CS1591 | compilação sem implicit usings; snapshots |
| 4. Compilação final validada + TDD zerados | 5, 9, 13 | DF4, DF9, Q5 | zero `KnownLimitation` restante (ou 1 convertido) | `dotnet test` sem filtro |
| 5. Semântica de null definida | 12 | DF5, Q3 | `From` sem NRE nos casos da política | testes `NullPolicy*` + Demo |
| 6. Pipeline cache-friendly | 14 | — | steps `Cached` após edição irrelevante | teste de steps incrementais |
| 7. CI com gates | 6 | DF9, Q4 | release só com CI verde | execução de workflows |
| 8. Documentação fiel | 1 | DF10 | README sem exemplo que gera RCSS003 | teste de compilação de snippets |

---

## Invariantes a preservar

1. Expressões geradas permanecem traduzíveis por EF Core — suíte do Demo (SQLite) verde em toda fase.
2. Nomes dos membros gerados (`Select{TFrom}Expression`, `From`, `Select{Dto}`, `To{Dto}`) não mudam sem decisão fechada.
3. Generator permanece netstandard2.0 e livre de dependências fora de `Microsoft.CodeAnalysis.*` + `RoyalCode.Extensions.SourceGenerator`.
4. Golden tests nunca são atualizados no mesmo commit que altera o emissor sem diff revisado por humano ou justificativa no `Resultado da Fase`.
5. Testes TDD de limitação nunca recebem `Skip` (DF9).
6. API pública dos atributos só muda com Q2 respondida.

---

## Critérios globais de conclusão

- `dotnet build SmartSelector.sln` sem warnings CS8618/CS0108 originados de código do repositório ou gerado.
- `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj` verde sem filtro (ressalva Q5=B documentada).
- `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj` verde.
- Harness de casos-limite (B1–B5, órfão, fully-qualified, homônimos) sem falha silenciosa nem erro CS inesperado.
- CI executando gate em PR; release condicionado a CI verde.
- `Perguntas ao humano` vazia ou explicitamente diferida.

---

## Riscos

| Risco | Gatilho | Impacto | Mitigação | Estado |
|---|---|---|---|---|
| Piso Roslyn 5.6 exclui consumidores net8/net9 | publicação do pacote antes de Q1 | pacote inutilizável para parte do público | decidir Q1 na Fase 3, antes de qualquer release | Aberto |
| Mudanças no pacote externo bloqueiam Fases 10–14 | `TypeDescriptor`/`ClassGenerator` sem API necessária | fases bloqueadas aguardando release externo | identificar necessidades cedo; abrir issues no repositório do pacote na Fase 8 | Aberto |
| Atualização em massa de snapshots mascara regressão | Fases 7, 11, 12 alteram todos os golden tests | código gerado incorreto aprovado | commit separado para snapshots + validação de compilação da Fase 5 | Aberto |
| Condicionais de null quebram tradução EF em provedores específicos | Fase 12 | queries falham em runtime do consumidor | validar via Demo SQLite; documentar provedores testados | Aberto |
| Gate de CI bloqueado pelos TDD vermelhos | Fase 6 ativada antes da categorização | CI vermelho permanente | Fase 5 (traits) obrigatória antes da Fase 6 | Aberto |

---

## Diferidos e backlog

- Mapeamento reverso (`ToEntity`/`Update(entity)`) — destino: backlog (avaliar após Fase 15).
- Suporte a records e membros `init`/`required` — destino: backlog.
- Interfaces como tipo de origem (`TypeKind.Class` exigido hoje) — destino: backlog.
- CodeFix providers (adicionar `partial`, criar propriedade correspondente) — destino: backlog.
- Renomear `{Dto}_Extensions` → `{Dto}Extensions` (CA1707) — destino: backlog, condicionado a Q2.
- Transformar snippets do README em testes compiláveis além do mínimo da Fase 1 — destino: fase futura de docs.

---

## Referências

- `.ai/reviews/smart-selector-review-2026-07-10.md` — revisão técnica anterior (itens 1–8).
- `.ai/templates/template-ai-implementation-plan.md` — template deste plano.
- `RoyalCode.SmartSelector.Tests/Tests/GeneratedCodeCompilationTddTests.cs` — testes TDD de limitações conhecidas.
- `RoyalCode.SmartSelector.Generators/AnalyzerDiagnostics.cs` — IDs RCSS000–005 atuais.
- `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md` — rastreio de diagnósticos.
- Análise de código de 2026-07-11 (conversa Claude Code) — bugs B1–B5 e harness de casos-limite.
