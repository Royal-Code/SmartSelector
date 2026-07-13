# Plan: Endurecimento e evolução do SmartSelector (`smartselector-hardening`)

## Status: CONCLUÍDO - Fases 0–15 concluídas

## Progresso

`████████████████` **100%** - 16 de 16 fases

| Fase | Estado |
|---|---|
| Fase 0 - Spike de viabilidade externa e matriz de compatibilidade | Concluida |
| Fase 1 - Harness de testes com validação de compilação | Concluida |
| Fase 2 - Typos e documentação | Concluida |
| Fase 3 - Limpeza de Demo e Benchmarks | Concluida |
| Fase 4 - Bugs de geração de baixo risco | Concluída em 2026-07-11 |
| Fase 5 - Diagnósticos completos e localizados | Concluída em 2026-07-11 |
| Fase 6 - AutoProperties<T> semântico | Concluída em 2026-07-12 |
| Fase 7 - Refactors internos do generator | Concluída em 2026-07-12 |
| Fase 8 - Empacotamento e dependências | Concluída em 2026-07-12 |
| Fase 9 - CI e release com gates | Concluída em 2026-07-12 (release gated revertido por decisão do humano) |
| Fase 10 - Contrato do AutoDetails | Concluída em 2026-07-12 |
| Fase 11 - Código gerado auto-suficiente e nullable-clean | Concluída em 2026-07-12 |
| Fase 12 - Política de null em From e coleções | Concluída em 2026-07-12 |
| Fase 13 - DTOs aninhados e diagnóstico permanente para genéricos | Concluída em 2026-07-12 |
| Fase 14 - Pipeline incremental sem retenção de símbolos | Concluída em 2026-07-12 |
| Fase 15 - Features incrementais de mapeamento | Concluída em 2026-07-12 |

> **Manutenção deste plano:** ao concluir as tarefas de uma fase, marque cada tarefa com `- [x]`,
> troque o **Estado** da fase para `Concluida` na tabela acima e atualize a barra de progresso
> (um bloco `█` por fase concluída, `%` e `X de N`). Exemplo de barra: `███░░░░░░░░░░░░░`.
> Antes de fechar uma fase, confirme que decisões, critérios de aceite, testes e invariantes relacionados foram aplicados.

---

## Contexto

### Fontes verificadas

- `.ai/reviews/smart-selector-review-2026-07-10.md` — revisão técnica anterior com 8 problemas pendentes e recomendações (política de null, contrato AutoDetails, harness, DTOs genéricos/aninhados, AutoProperties sintático, nullable-clean, CI, README).
- Análise externa do plano (2026-07-11) — 10 achados avaliados; 7 válidos, 2 parcialmente válidos, 1 rejeitado pelo humano (ver Histórico de decisões).
- Segunda análise externa do plano (2026-07-11) — 10 achados avaliados, todos válidos; ajustes aplicados (ver Histórico de decisões).
- `dotnet build SmartSelector.sln` (2026-07-11) — compila com 0 erros e 22 warnings (CS8618 no Demo/Library, CS0108 em `Tests/Models/Expected/BlogsPosts.cs`, CS8601/CS8604 em `Tests/Models/Expected/Nulls.cs`).
- `dotnet test RoyalCode.SmartSelector.Tests` (2026-07-11) — 33 aprovados, 3 reprovados (os 3 de `GeneratedCodeCompilationTddTests`, vermelhos por design).
- `dotnet test RoyalCode.SmartSelector.Demo` (2026-07-11) — 25/25 aprovados (EF Core + SQLite).
- Harness de casos-limite executado contra o generator compilado (2026-07-11) — confirmou os bugs B1, B2, B3, B4 e o silêncio de `[AutoProperties]` órfão; confirmou que `Exclude = null` não quebra.

### Estado atual do código (verificado em 2026-07-11)

- **B1 — colisão de hintName:** dois DTOs com mesmo nome em namespaces diferentes causam `ArgumentException` (hintName `Details.g.cs` duplicado) e CS8785; nenhum código é gerado. hintName definido só pelo nome da classe em `AutoSelectGenerator.Generate` e `AutoPropertiesGenerator.cs:362`.
- **B2 — AutoDetails com nome fora da convenção:** `AutoDetailsGenerator.cs:54` nomeia a classe gerada como `{TipoOrigem}Details`; propriedade declarada como `AddressDto` gera `AddressDetails` e a expressão referencia `AddressDto` inexistente → CS0246.
- **B3 — propriedade get-only duplicada:** filtro de exclusão em `AutoPropertiesGenerator.cs:182` usa `p.SetMethod is not null`; `public string Name => "x";` no DTO não é excluída → CS0102 no código gerado.
- **B4 — dependência de ImplicitUsings:** arquivos gerados não emitem `using System;`/`System.Linq`/`System.Collections.Generic`; compilação sem implicit usings falha com CS0246 para `Func<,>`, `IQueryable<>`, `IEnumerable<>`.
- **B5 — herança de DTOs:** `PostAndCommentsDetails : PostDetails` (ambos `AutoSelect<Post>`) gera membros públicos (`SelectPostExpression`, `From`) que ocultam os herdados → CS0108 no consumidor. O campo privado gerado **não** participa do conflito (verificado no build: CS0108 apenas nos 2 membros públicos); `new` no campo produziria CS0109.
- **Diagnósticos incorretos/silêncio:** classe não-partial com `AutoProperties<T>` reporta RCSS005 com mensagem de type argument; `[AutoProperties]` sem `AutoSelect` não gera nada nem diagnostica; `AutoDetailsGenerator.cs:33` cria diagnóstico com `location: null`; o nome incorreto citado anteriormente pela mensagem RCSS003 foi corrigido na Fase 2.
- **AutoProperties<T> sintático:** `AutoPropertiesGenerator.cs:54-57` filtra por `IdentifierNameSyntax`/`GenericNameSyntax`; forma qualificada (`[global::...AutoProperties<X>]`) é ignorada silenciosamente (teste TDD vermelho).
- **DTO genérico/aninhado:** geração produz declaração de nível de namespace com nome simples; 2 testes TDD vermelhos.
- **Retenção de símbolos no pipeline:** `TypeDescriptor` (pacote externo `RoyalCode.Extensions.SourceGenerator` 0.1.13) carrega `ISymbol`; os `*Information` carregam `Diagnostic[]` (com `Location`→`SyntaxTree`); mutações durante geração em `SelectLambdaGenerator.cs:109` (`AddParentProperty`) e `AutoDetailsGenerator.cs:47` (`Namespaces[0] = ...`).
- **Roslyn 5.6.0 no generator:** `RoyalCode.SmartSelector.Generators.csproj:21-22`; eleva o piso do consumidor para SDK .NET 10.0.3xx+, conflitando com o suporte declarado a net8.0/net9.0 da lib runtime.
- **README/docs divergentes:** `README.md:147` combina `AutoSelect<Order>` com `AutoProperties<Order>` (gera RCSS003); `README.md:8` tem seta corrompida (`?`); README/docs mostram `SelectXxxExpression =>` mas o código gerado usa `{ get; } =`.
- **CI:** workflow manual, sem execução de testes, sem gate, publica todo `.nupkg` por glob; nomes de etapas referenciam SmartSearch (fonte: revisão 2026-07-10, item 7).
- **Benchmarks:** `Generated_From` e `Generated_CachedDelegate` idênticos; descrição "AutoMapper ProjectTo List" usa `Map` por item; `var now` sem uso; `Console.WriteLine("Hello, World!")` em `Program.cs:4`.

### Lacunas, conflitos e restrições

- **Dependência do pacote externo `RoyalCode.Extensions.SourceGenerator`:** `TypeDescriptor`, `MatchSelection`, `ClassGenerator` etc. vivem fora deste repositório. O código-fonte está disponível localmente em `C:\git\RoyalCode\Utils\RoyalCode.Utils\RoyalCode.Extensions.SourceGenerator` (testes em `...\RoyalCode.Extensions.SourceGenerator.Tests`, solution `C:\git\RoyalCode\Utils\RoyalCode.Utils\Util.sln`) e pode ser consultado e modificado; toda modificação exige release de nova versão NuGet (hoje 0.1.13). A Fase 0 antecipa essa descoberta.
- **`TRUSTED_PLATFORM_ASSEMBLIES` não valida TFMs antigos:** o TPA entrega assemblies de implementação do runtime do test host (net10); compilar contra ele não prova compatibilidade com net8/net9. Testes de compilação por TFM exigem reference assemblies; o piso de SDK do generator só é verificável em matriz de SDKs reais (Fase 0).
- **Golden tests comparam texto integral:** qualquer mudança de emissão (usings, cabeçalho, XML docs) altera todos os snapshots de uma vez; regressões podem passar despercebidas em atualizações em massa.
- **Testes TDD vermelhos por design:** qualquer gate de CI precisa categorizá-los por trait antes de existir, com governança para a dívida não apodrecer.

### Superfícies impactadas a mapear

- `RoyalCode.SmartSelector` (pacote runtime) — atributos públicos; mudanças são contrato público NuGet.
- `RoyalCode.SmartSelector.Generators` (pacote analyzer) — forma do código gerado é contrato de fato dos consumidores.
- `RoyalCode.Extensions.SourceGenerator` (repositório local `C:\git\RoyalCode\Utils\RoyalCode.Utils`, solution `Util.sln`) — mudanças em `TypeDescriptor`/geradores exigem release de nova versão NuGet.
- `.github/workflows` — publicação NuGet.

---

## Objetivo

1. Zero bugs confirmados (B1–B5) reproduzíveis pelo harness de casos-limite.
2. Nenhum cenário não suportado falha em silêncio: todo caso rejeitado emite diagnóstico RCSS localizado.
3. Código gerado compila sem depender de `ImplicitUsings`, é nullable-clean e carrega cabeçalho auto-generated.
4. Suíte de testes valida a compilação final (erros CS) por padrão, contra reference assemblies dos TFMs suportados; os 3 testes TDD ficam verdes ou cobertos por diagnóstico.
5. `From`/extensões `IEnumerable` têm semântica de null definida, documentada e testada em memória e via EF Core SQLite.
6. Pipeline incremental sem retenção de `ISymbol`/`Diagnostic` nos modelos cacheados e sem mutação durante a geração.
7. CI executa build+testes como gate; release publica somente artefatos validados.
8. Documentação (README/docs.md) fiel ao código gerado e sem exemplos que produzem erro.

## Fora de escopo

- Mapeamento reverso (`ToEntity`/`Update(entity)`) — rejeitado permanentemente (DF12); o pacote é de projeção (`Select`), não cria entidades a partir de DTOs.
- Renomear `{Dto}_Extensions` para `{Dto}Extensions` — fora deste plano; permanece apenas como item de backlog para plano futuro.
- Suporte a records, `init`/`required` members — destino: backlog.
- Interfaces como tipo de origem — destino: backlog.
- CodeFix providers para RCSS000/001 — destino: backlog.

---

## Decisões fechadas

- **DF1 — hintName com identidade completa:** hintName dos arquivos gerados inclui namespace, cadeia de containing types, nome do DTO, aridade genérica (future-proofing, mesmo com DF20) e categoria do artefato (ex.: `App.First.Details.AutoSelect.g.cs`, `App.First.Details.Extensions.g.cs`, `App.Second.Details.AutoProperties.g.cs`). Fonte: bug B1 confirmado por harness em 2026-07-11 + segunda análise externa 2026-07-11, achado 3 (colisão de aninhados homônimos após a Fase 13).
- **DF2 — AutoDetails gera o tipo declarado na propriedade:** o nome/namespace da classe gerada vem de `property.Type`, não de `{TipoOrigem}Details`. Fonte: revisão 2026-07-10, item 2, opção A.
- **DF3 — AutoProperties<T> resolvido semanticamente:** usar `context.Attributes`/`AttributeData` para tipo e named arguments; sintaxe apenas para localização de diagnóstico. Fonte: revisão 2026-07-10, item 5, opção A.
- **DF4 — Harness devolve resultado estruturado e valida por TFM:** `Util.Compile` evolui para resultado com `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources`; validação de erros de compilação vira padrão; testes de compilação usam reference assemblies dos TFMs suportados (TPA permitido apenas como caminho rápido interno). Fonte: revisão 2026-07-10, item 3, opção A + análise externa 2026-07-11, achado 2.
- **DF5 — Política de null direcional:** nullable → nullable propaga `null` via condicional na expression tree; nullable → non-nullable escalar emite diagnóstico warning. Fonte: revisão 2026-07-10, item 1, opção A (fallback de coleção definido em DF18).
- **DF6 — Nullable-clean por modelagem:** campo de cache vira `Func<TFrom, TDto>?`; sem `#nullable disable` global no código gerado. Fonte: revisão 2026-07-10, item 6, opção A.
- **DF7 — DTOs genéricos/aninhados: diagnóstico primeiro, suporte depois:** Fase 5 emite diagnósticos temporários; Fase 13 implementa suporte a aninhados. Fonte: revisão 2026-07-10, item 4.
- **DF8 — Idioma:** comentários de código em português; identificadores, diagnósticos e XML docs públicos em inglês. Fonte: predominância verificada no código atual.
- **DF9 — Testes TDD nunca recebem `Skip` e a dívida é governada por baseline nominal:** categorização por trait explícita (`Category=KnownLimitation`), cada limitação vinculada à fase que a remove, proibição de novas limitações sem decisão registrada neste plano. O CI valida uma baseline **por nome** (Fase 1: 3 limitações; após Fase 6: 2; após Fase 13: 0) e falha se a quantidade aumentar, se surgir nome não registrado ou se um teste conhecido passar (sinal de que deve sair da lista); a falha dos testes conhecidos em si não bloqueia. Fonte: revisão 2026-07-10, item 7 + análises externas 2026-07-11 (achados 9 e 6).
- **DF10 — README corrigido e verificado:** corrigir exemplo contraditório e criar teste de compilação para os snippets principais. Fonte: revisão 2026-07-10, item 8, opções A+B.
- **DF11 — Cabeçalho e metadados de geração gradados:** todo arquivo gerado recebe `// <auto-generated/>`. `[GeneratedCode]` no tipo apenas para classes **sem nenhuma declaração escrita pelo usuário**: `{Dto}_Extensions` e classes de AutoDetails geradas do zero. Quando existe declaração do usuário (DTOs parciais e AutoDetails que completa tipo `partial` preexistente — caso da Fase 10), o atributo vai somente nos membros gerados (atributo em declaração partial contamina o tipo inteiro). Membros públicos gerados recebem XML docs. Fonte: análise 2026-07-11 + análises externas 2026-07-11 (achados 6 e 2 da segunda).
- **DF12 — Sem mapeamento reverso:** o pacote é de projeção (`Select`); geração de entidades a partir de DTOs (`ToEntity`/`Update(entity)`) não será implementada. Fonte: decisão humana em 2026-07-11.
- **DF13 — `new` somente em membros públicos conflitantes, detectado semanticamente:** o modificador `new` é emitido em `Select{TFrom}Expression` e `From` apenas quando a classe base do DTO declara membro homônimo acessível; o campo privado nunca recebe `new` (CS0109). Fonte: análise externa 2026-07-11, achado 3, verificado contra o build.
- **DF14 — Correções nullable do Demo por modelagem:** eliminar CS8618 do Demo com `required`, construtores ou `= default!` (POCOs materializados pelo EF); não usar `#nullable disable`. Fonte: análise externa 2026-07-11, achado 10.
- **DF15 — Empacotamento do generator multi-target por versão de Roslyn:** empacotar builds do generator em `analyzers/dotnet/roslyn4.8/cs` e `analyzers/dotnet/roslyn5.6/cs`. A Fase 0 confirmou build, seleção da variante e execução com SDKs 8.0.422, 9.0.100 e 10.0.301. Fonte: Q1, resposta humana 2026-07-11 + spike reproduzível de 2026-07-11.
- **DF16 — Atributos selados:** adicionar `sealed` a `AutoSelectAttribute<T>`, `AutoPropertiesAttribute`, `AutoPropertiesAttribute<T>` e `AutoDetailsAttribute`. Fonte: Q2a, resposta humana 2026-07-11 (opção A).
- **DF17 — Classe base para `Exclude`/`Flattening`:** extrair base abstrata pública consolidando as propriedades duplicadas nos atributos. Fonte: Q2b, resposta humana 2026-07-11 (opção A).
- **DF18 — Coleção nullable → destino non-nullable usa coleção vazia + diagnóstico informativo:** gerar fallback de coleção vazia (`... == null ? new List<T>() : ...` ou equivalente traduzível) e emitir diagnóstico de severidade Info apontando a conversão. Fonte: Q3, resposta humana 2026-07-11 (opção A com diagnóstico de informação).
- **DF19 — Release manual com aprovação:** workflow de release via `workflow_dispatch` com aprovação, consumindo somente artefatos de CI validado. Fonte: Q4, resposta humana 2026-07-11 (opção B). **Revertida pelo humano em 2026-07-12:** o release voltou a ser o workflow manual simples `smart-select.yml` (build + pack + push); ver Resultado da Fase 9.
- **DF20 — DTO genérico rejeitado com diagnóstico permanente, incluindo containing types genéricos:** entidades não terão tipos genéricos; `EntityDetails<T>` recebe diagnóstico permanente e o teste TDD correspondente é convertido em teste de diagnóstico. O mesmo diagnóstico se aplica quando qualquer containing type do DTO aninhado é genérico (`Container<T>.EntityDetails`). Nota técnica registrada para eventual suporte futuro: declarações `partial` podem omitir constraints (basta repetir nomes e aridade dos type parameters), o que barateia o suporte se a decisão mudar. Fonte: Q5, resposta humana 2026-07-11 (opção B) + segunda análise externa 2026-07-11, achado 9.

---

## Histórico de decisões

**Q1–Q5 (respondidas pelo humano em 2026-07-11):**

- **Q1 — Piso de Roslyn:** opções eram A) reduzir para 4.8.0 ou B) manter 5.6.0.
  - **Resposta:** avaliar se é possível definir uma versão por versão do .NET; se não der, opção A.
  - **Considerações:** existe mecanismo oficial de multi-targeting de analyzers por pasta `analyzers/dotnet/roslyn{X.Y}/cs`; viabilidade prática será confirmada na Fase 0.
  - **Conclusão:** fechada como DF15.
- **Q2a — Selar atributos:** **Resposta:** opção A. **Conclusão:** DF16.
- **Q2b — Classe base `Exclude`/`Flattening`:** **Resposta:** opção A. **Conclusão:** DF17.
- **Q3 — Coleção nullable → non-nullable:** opções eram A) coleção vazia, B) propagar null + warning, C) erro.
  - **Resposta:** opção A, com diagnóstico de informação (não warning).
  - **Conclusão:** DF18.
- **Q4 — Estratégia de release:** **Resposta:** opção B (manual com aprovação). **Conclusão:** DF19.
- **Q5 — DTOs genéricos:** **Resposta:** opção B — entidades não terão tipos genéricos. **Conclusão:** DF20.

**Planejamento (escopo):**

- **Mapeamento reverso (`ToEntity`/`Update(entity)`):** proposto como item de backlog na análise de 2026-07-11.
  - **Resposta:** humano rejeitou em 2026-07-11 — o pacote é `Select`; não deve criar entidades a partir de DTOs.
  - **Conclusão:** removido do backlog; fechado como DF12 (fora de escopo permanente).

**Planejamento (revisão externa de 2026-07-11):**

- **Achados 1, 3, 5, 6, 7, 9, 10 — aceitos:** reordenação de fases (harness como primeira fase técnica, refactors após estabilizar comportamento, Fase 0 de spike), correção da tarefa B5 (DF13), gradação de `[GeneratedCode]` (DF11 atualizada), divisão de Q2 em Q2a/Q2b (rename movido para fora do plano), governança do trait `KnownLimitation` (DF9 atualizada), remoção da alternativa `#nullable disable` no Demo (DF14).
- **Achados 2 e 8 — parcialmente aceitos:** TPA mantido como caminho rápido, com reference assemblies por TFM e matriz de SDKs na Fase 0 (DF4 atualizada); dependências passam a ser por tarefa nas Fases 8 e 9 (Q4 já estava granular).
- **Achado 4 (separar features em outro plano) — REJEITADO pelo humano:** as features permanecem na Fase 15 deste plano. O item 13 da ordem sugerida pela análise externa foi descartado em consequência.

**Planejamento (segunda revisão externa de 2026-07-11 — 10 achados, todos válidos):**

- **Achado 1 (comandos de teste antes da Fase 13):** aceito — gates das Fases 6–12 usam `--filter "Category!=KnownLimitation"`; suíte sem filtro vira gate a partir da Fase 13.
- **Achado 2 (`[GeneratedCode]` em AutoDetails preexistente):** aceito — DF11 refinada: atributo no tipo só quando não há nenhuma declaração do usuário.
- **Achado 3 (hintName com containing types):** aceito — DF1 atualizada com cadeia de containing types, aridade e categoria do artefato.
- **Achado 4 (DF10 sem tarefa):** aceito — tarefa de compilação de snippets adicionada à Fase 2 (usa o harness da Fase 1).
- **Achado 5 (dependência externa por variante Roslyn):** aceito — Fase 0 verifica o Roslyn de compilação do pacote externo, dependências por pasta e DLL carregada via binlog.
- **Achado 6 (baseline nominal de limitações):** aceito — DF9 atualizada: baseline por nome (3 → 2 → 0) validada no CI.
- **Achado 7 (procedência do release):** aceito — Fase 9: `ci_run_id` obrigatório, validação de status/branch/SHA, download sem rebuild.
- **Achado 8 (verificação de API pública):** aceito — Fase 7: PublicApiAnalyzers, teste de consumidor derivando, versão alvo 0.5.0 e nota de migração.
- **Achado 9 (containing types genéricos):** aceito — DF20 estendida: diagnóstico permanente também para containing type genérico; nuance registrada (partial pode omitir constraints, barateando suporte futuro).
- **Achado 10 (retenção indireta):** aceito — critério da Fase 14 passou a ser transitivo, com teste de reflexão sobre o grafo cacheado.

### Revisão pós-Fase 0 (SDK 8 instalado, 2026-07-11)

- **Matriz DF15 fechada:** SDK 8.0.422/Roslyn 4.8 selecionou `roslyn4.8`; SDK 9.0.100/Roslyn 4.12 selecionou `roslyn4.8`; SDK 10.0.301/Roslyn 5.6 selecionou `roslyn5.6`. Build e execução passaram nos três casos.
- **Releases externos separados por risco:** 0.1.14 para compatibilidade Roslyn e higiene crítica (equality/hash e caches estáticos); 0.1.15 para capacidades de emissão (nullable e containing types); 0.2.0 para o modelo potencialmente breaking sem símbolos.
- **Nullable decidido como dependência externa:** a Fase 11 consumirá `NullableAnnotation` modelada no `TypeDescriptor` 0.1.15; não será criado um segundo modelo local paliativo.
- **Spike preservado de forma reproduzível:** fontes e script ficam em `scratchpad/spike/`; artefatos pesados são regeneráveis e ignorados; `last-run.md` registra matriz e hash do pacote.

---

## Design alvo

### Contratos e bordas

- `RoyalCode.SmartSelector` (atributos): `AutoSelectAttribute<TFrom>`, `AutoPropertiesAttribute(<TFrom>)`, `AutoDetailsAttribute`, `MapFromAttribute` — assinatura estável salvo o aprovado em DF16 (sealed) e DF17 (classe base).
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
  compilação contra reference assemblies por TFM (net8/net9/net10); TPA só como caminho rápido
  golden tests + testes de diagnóstico + traits governados para limitações conhecidas

.github/workflows/
  ci.yml: build + test (filtro KnownLimitation + relatório de contagem) + pack em PR/push
  release.yml: publica artefatos do CI validado
```

### Segurança, concorrência e confiabilidade

- Expressões geradas permanecem traduzíveis por EF Core; toda mudança de emissão passa pelos testes do Demo (SQLite).
- Cache do delegate `From` permanece por tipo, estático, com atribuição idempotente (`??=`).
- Release publica somente artefatos produzidos por run de CI verde (após Fase 9).

### Compatibilidade, migração e rollout

- Compatibilidade de Roslyn resolvida por DF15 (`roslyn4.8` + `roslyn5.6`), validada pela matriz de SDKs 8/9/10 da Fase 0; documentar SDK mínimo no README.
- Mudanças na forma do código gerado (Fases 11–12) são observáveis por consumidores: registrar no changelog/release notes.
- Breaking de API pública aprovado por DF16/DF17 (selar atributos, classe base); versão 0.x permite, mas exige nota de release.

---

## Ordem de execução

1. **Fase 0 (Spike de viabilidade)** — de-riska Fases 10, 11, 13, 14 e valida DF15 (multi-target Roslyn) antes de qualquer release.
2. **Fase 1 (Harness de testes)** — primeira fase técnica; protege todas as fases de comportamento.
3. **Fase 2 (Typos e documentação)** — trivial, sem dependências.
4. **Fase 3 (Limpeza Demo/Benchmarks)** — trivial, sem dependências.
5. **Fase 4 (Bugs de baixo risco)** — correções pontuais já protegidas pelo harness.
6. **Fase 5 (Diagnósticos)** — elimina falhas silenciosas antes de mudanças maiores.
7. **Fase 6 (AutoProperties semântico)** — zera 1 teste TDD; remove o caminho sintático antes dos refactors.
8. **Fase 7 (Refactors internos)** — após comportamento estabilizado; consolidação não é retrabalhada; aplica DF16/DF17.
9. **Fase 8 (Empacotamento e dependências)** — aplica DF15 com o resultado da Fase 0.
10. **Fase 9 (CI e release)** — CI de validação + release manual com aprovação (DF19).
11. **Fase 10 (Contrato AutoDetails)** — depende de DF2, Fase 5 e viabilidade da Fase 0.
12. **Fase 11 (Código gerado auto-suficiente/nullable-clean)** — mexe em todos os snapshots; exige Fases 1 e 4 concluídas.
13. **Fase 12 (Política de null)** — aplica DF5 e DF18; depende da Fase 11.
14. **Fase 13 (Aninhados + diagnóstico permanente para genéricos)** — aplica DF20; depende da Fase 5 e da viabilidade da Fase 0.
15. **Fase 14 (Pipeline sem símbolos)** — refactor mais profundo; usa o mapeamento da Fase 0.
16. **Fase 15 (Features de mapeamento)** — sobre base estabilizada; permanece neste plano por decisão do humano.

Build/test padrão:

```powershell
dotnet build SmartSelector.sln
dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj
dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj
```

---

## Fase 0 - Spike de viabilidade externa e matriz de compatibilidade

**Depende de:** nada.

**Escopo:** somente leitura/investigação; código-fonte local do `RoyalCode.Extensions.SourceGenerator` (`C:\git\RoyalCode\Utils\RoyalCode.Utils\RoyalCode.Extensions.SourceGenerator`, testes em `...\RoyalCode.Extensions.SourceGenerator.Tests`, solution `C:\git\RoyalCode\Utils\RoyalCode.Utils\Util.sln`), pack local do generator, projetos de consumo temporários. Nenhuma mudança em código de produção.

**O que/como:** investigação time-boxed para responder as incógnitas que bloqueiam fases futuras, lendo o código-fonte local do pacote externo; saída é um relatório em `.ai/reviews/` com a lista de mudanças externas necessárias (cada uma exigirá release NuGet do pacote) e a validação de DF15.

**Tarefas:**

- [x] Verificar no código-fonte local se `ClassGenerator` suporta containing types; registrar o que falta para a Fase 13 (aninhados; genéricos dispensados por DF20).
- [x] Verificar se a emissão suporta cabeçalho de arquivo, XML docs e anotações nullable (`NullableAnnotation` em `TypeDescriptor`); registrar o que falta para a Fase 11.
- [x] Mapear onde `TypeDescriptor`/`MatchSelection` retêm `ISymbol` e o que a Fase 14 exige do pacote externo.
- [x] Classificar cada mudança necessária como "local (SmartSelector)" ou "externa (release NuGet do RoyalCode.Extensions.SourceGenerator)"; listar as externas no relatório com a versão mínima alvo (>= 0.1.14).
- [x] Verificar contra qual versão de `Microsoft.CodeAnalysis` o `RoyalCode.Extensions.SourceGenerator` é compilado (código-fonte local) e se a mesma DLL carrega sob Roslyn 4.8 e 5.6, ou se serão necessárias duas variantes do pacote externo.
- [x] Validar DF15: pack local do generator multi-target (`analyzers/dotnet/roslyn4.8/cs` + `analyzers/dotnet/roslyn5.6/cs`), com **todas as dependências da variante na mesma pasta** (incluindo `RoyalCode.Extensions.SourceGenerator.dll`), e verificar se cada SDK (8.0.x, 9.0.x, 10.0.x) carrega a build correta — confirmar qual DLL foi efetivamente carregada via binlog (`dotnet build -bl`); registrar a matriz de resultados.
- [x] Se o multi-target for inviável, registrar a evidência e confirmar o fallback de DF15 (downgrade para 4.8.0). *(N/A — multi-target viável.)*
- [x] Publicar o relatório do spike em `.ai/reviews/spike-viabilidade-externa-<data>.md`.

**Critérios de aceite:** relatório existe e responde às quatro incógnitas (containing types, emissão, símbolos, matriz SDK/multi-target); DF15 tem evidência empírica de viabilidade ou fallback confirmado; mudanças externas listadas com versão alvo.

**Testes:** não aplicável (spike); a matriz de SDKs é a própria verificação.

### Resultado da Fase 0

**Concluída em 2026-07-11.** Relatório: `.ai/reviews/spike-viabilidade-externa-2026-07-11.md`.

- **Entregáveis:** relatório do spike; experimento reproduzível em `scratchpad/spike/` (variantes 4.8/5.6, pacote `SpikeGen`, consumidor, logs diagnósticos com `/analyzer:`).
- **DF15 VIÁVEL, sem fallback:** generator e pacote externo compilam contra Roslyn 4.8.0 **sem nenhum erro** (nenhuma API nova usada). Matriz completa: SDK 8.0.422 (Roslyn 4.8) selecionou `roslyn4.8`; SDK 9.0.100 (Roslyn 4.12) selecionou `roslyn4.8`; SDK 10.0.301 (Roslyn 5.6) selecionou `roslyn5.6`. Build e execução OK nos três casos.
- **Condição para a Fase 8:** o 0.1.13 publicado do externo compila contra MC **4.14** e não carrega sob compiladores < 4.14; release **0.1.14 com pin 4.8.0** é pré-requisito da pasta `roslyn4.8` (a mesma DLL serve às duas pastas).
- **Fase 11 é majoritariamente local:** header via evento `Generating`; `[GeneratedCode]` via `ClassGenerator.Attributes`/`MethodGenerator.Attributes`; membros sem suporte a atributos/docs (`PropertyGenerator`/`FieldGenerator`) resolvíveis por subclasse local. A nulabilidade será resolvida externamente por `NullableAnnotation` no `TypeDescriptor` 0.1.15.
- **Fase 13 bloqueada em release externo:** `ClassGenerator` não suporta containing types (mudança externa, 0.1.15).
- **Higiene crítica antecipada para o externo 0.1.14:** corrigir equality/hash e remover os três caches estáticos que retêm Compilations (`TypeDescriptor.voidTypeDescriptor`, `TypeDescriptor.cancellationToken` e `ParameterDescriptor.cancellationToken`). `PropertyDescriptor.Equals(object)` também pode recursar até `StackOverflowException` quando recebe `ParameterDescriptor`.
- **Fase 14 exige release externo dedicado (0.2.0):** remover retenção direta e transitiva de símbolos em `TypeDescriptor`/`PropertyDescriptor`/`MatchSelection` com um modelo imutável; a versão major/minor separada admite alterações incompatíveis.
- **Desvios/achados extras:** DTO em namespace global gera código inválido — registrar diagnóstico nas Fases 4/5.
- **Pendências:** automatizar a matriz 8/9/10 no CI como regressão; solicitar o externo 0.1.14 antes da Fase 8 e o 0.1.15 antes das Fases 11 e 13.

---

## Fase 1 - Harness de testes com validação de compilação

**Depende de:** DF4, DF9.

**Escopo:** `RoyalCode.SmartSelector.Tests/Util.cs`, todos os arquivos de teste, `GeneratedCodeCompilationTddTests.cs`, `tests.targets` (pacote de reference assemblies).

**O que/como:** implementar o resultado estruturado (revisão item 3, opção A) com validação por TFM e migrar os testes existentes.

**Tarefas:**

- [x] Criar `CompileResult` com `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources` (por hintName) e `RunResult`.
- [x] Adotar reference assemblies por TFM para os testes de compilação (ex.: pacote `Basic.Reference.Assemblies` para net8/net9/net10); manter TPA apenas como caminho rápido explícito para testes internos.
- [x] Migrar todos os testes para o novo helper, mantendo as asserções de snapshot por hintName em vez de índice de árvore (`SyntaxTrees.Skip(n)`).
- [x] Tornar padrão a asserção "zero erros de compilação final"; asserção opcional de "zero warnings".
- [x] Marcar os 3 testes TDD com trait `Category=KnownLimitation` (sem `Skip`), cada um com comentário apontando a fase deste plano que o tornará verde (Fase 6: fully-qualified; Fase 13: aninhado e genérico).
- [x] Registrar a regra de governança: nova limitação só entra com decisão registrada neste plano (DF9).

**Critérios de aceite:** nenhum teste acessa `SyntaxTrees.Skip(n)`; todo teste de geração falha se o código gerado tiver erro CS em pelo menos um TFM suportado; `dotnet test --filter "Category!=KnownLimitation"` fica 100% verde; cada teste `KnownLimitation` referencia a fase que o resolve.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`.

### Resultado da Fase 1

**Concluída em 2026-07-11.**

- `CompileResult` passou a separar diagnósticos do generator e da compilação final, expor fontes por hint name e preservar `GeneratorDriverRunResult`.
- `CompileAndAssert` valida por padrão `net8.0`, `net9.0` e `net10.0`; warnings podem ser exigidos explicitamente. `CompileFast` mantém TPA como caminho deliberado e documentado.
- Reference assemblies vêm de `Basic.Reference.Assemblies.Net80` 1.8.9, `Net90` 1.8.9 e `Net100` 1.8.8, isolados por aliases. As fontes atuais dos quatro atributos runtime são recursos embutidos e compiladas em cada TFM, evitando referenciar a DLL do test host `net10.0` durante validações de TFMs anteriores.
- Todos os snapshots exatos usam hint names; não resta acesso a `SyntaxTrees.Skip(n)`.
- Fixtures antes incompletas foram tornadas autocontidas (tipos ausentes e acessibilidade inconsistente), sem alteração no código de produção.
- Os três testes TDD têm `Category=KnownLimitation`, continuam falhando e apontam para as Fases 6/13. A governança permanece definida em DF9: nova limitação exige decisão registrada neste plano.
- Gate: `dotnet test RoyalCode.SmartSelector.Tests/RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"` — **33/33 aprovados**. Baseline informativa `Category=KnownLimitation` — **3/3 falhando como esperado**, nenhum `Skip`.

---

## Fase 2 - Typos e documentação

**Depende de:** DF10; Fase 1 (harness `CompileResult` para a tarefa de snippets).

**Escopo:** `README.md`, `docs.md`, `AnalyzerDiagnostics.cs`, `AutoSelectGenerator.cs`, `AutoPropertiesGenerator.cs`, `MapFromAttribute.cs`, `InternalVisible.cs`.

**O que/como:** correções de texto sem mudança de comportamento; a única mudança observável é a string da mensagem RCSS003.

**Tarefas:**

- [x] Corrigir a grafia de “extensão” em `AutoSelectGenerator.cs`.
- [x] Corrigir a mensagem RCSS003 para citar `AutoPropertiesAttribute` nas duas ocorrências e cobri-la com teste de regressão.
- [x] Corrigir a frase do comentário que processa propriedades com `AutoDetails` em `AutoPropertiesGenerator.cs`.
- [x] Corrigir a seta corrompida do exemplo de flattening em `README.md`.
- [x] Corrigir o exemplo contraditório que combinava `AutoSelect<Order>` com a forma genérica de `AutoProperties` para a forma não genérica.
- [x] Atualizar README/docs.md para mostrar `{ get; } =` no lugar de `=>` em `SelectXxxExpression`.
- [x] Corrigir recuo do item `10.` no sumário de `docs.md`.
- [x] Remover a supressão obsoleta do parâmetro de construtor em `MapFromAttribute.cs`.
- [x] Remover linha em branco inicial de `InternalVisible.cs`.
- [x] Criar testes de compilação para os snippets principais do README/docs usando o harness da Fase 1 (DF10): quickstart (`AutoSelect` + `AutoProperties`), `AutoProperties<TFrom>` isolado, DTO aninhado com `AutoDetails` e flattening — cada um sem RCSS e sem erro C# inesperado.

**Critérios de aceite:** busca textual não encontra as grafias e o nome de atributo incorretos anteriores nem a supressão obsoleta; README não combina `AutoProperties<Order>` com `AutoSelect<Order>`; snippets principais compilam via harness; build e testes permanecem verdes.

**Testes:** `dotnet build SmartSelector.sln`; `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`.

### Resultado da Fase 2

**Concluída em 2026-07-11.**

- Corrigidos comentários, mensagem RCSS003, seta de flattening, sumário e exemplos de expressão gerada em README/docs.
- O exemplo de exclusão agora combina `AutoSelect<Order>` com `AutoProperties(...)`, respeitando o contrato dos atributos.
- Removidas a supressão obsoleta em `MapFromAttribute.cs` e a linha inicial vazia em `InternalVisible.cs`.
- `DocumentationSnippetCompilationTests` cobre quatro cenários nos TFMs `net8.0`, `net9.0` e `net10.0`: quickstart, `AutoProperties<TFrom>` isolado, objeto aninhado com `AutoDetails` e flattening por convenção. Todos exigem ausência de RCSS e de erros C#.
- `AnalyzerDiagnosticMessageTests` protege o nome correto de `AutoPropertiesAttribute` na mensagem RCSS003.
- Gate: `dotnet build SmartSelector.sln` — **0 erros, 22 warnings já mapeados para fases posteriores**; `dotnet test ... --filter "Category!=KnownLimitation"` — **38/38 aprovados**.

---

## Fase 3 - Limpeza de Demo e Benchmarks

**Depende de:** DF14.

**Escopo:** `RoyalCode.SmartSelector.Demo/Entities/Library/*`, `Details/Library/BookDetails.cs`, `RoyalCode.SmartSelector.Benchmarks/*`.

**O que/como:** eliminar warnings e ruído dos projetos auxiliares sem alterar cenários testados; nulabilidade corrigida por modelagem (DF14), não por supressão.

**Tarefas:**

- [x] Zerar os 6 CS8618 do Demo em `Book.cs`, `Shelf.cs` e `BookDetails.cs` usando `required`, construtor ou `= default!` (POCO materializado pelo EF); não usar `#nullable disable`.
- [x] Remover `Console.WriteLine("Hello, World!")` de `Benchmarks/Program.cs:4`.
- [x] Remover variável `now` sem uso em `ProductMappingBenchmark.cs:22`.
- [x] Remover ou diferenciar o benchmark duplicado `Generated_CachedDelegate` (idêntico a `Generated_From`).
- [x] Corrigir descrição "AutoMapper ProjectTo List" para refletir `Map` por item (ou trocar a implementação para `ProjectTo`).

**Critérios de aceite:** `dotnet build` do Demo sem nenhum CS8618 e sem novos `#nullable disable`; benchmarks compilam; nenhuma dupla de benchmarks com corpo idêntico.

**Testes:** `dotnet build SmartSelector.sln`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`; `dotnet build RoyalCode.SmartSelector.Benchmarks\RoyalCode.SmartSelector.Benchmarks.csproj -c Release`.

### Resultado da Fase 3

**Concluída em 2026-07-11.**

- Os seis membros non-nullable do cenário Library (`Book`, `Shelf` e `BookDetails`) agora usam `required`; nenhum `#nullable disable` foi introduzido.
- Removidos o `Hello, World!`, a variável local sem uso e o benchmark `Generated_CachedDelegate`, que repetia exatamente `Generated_From`.
- A descrição do benchmark do AutoMapper agora declara `Map() per item`, refletindo a implementação real.
- `dotnet build SmartSelector.sln` — **0 erros, 16 warnings restantes**, todos fora do Demo e já mapeados para fases posteriores.
- `dotnet test RoyalCode.SmartSelector.Demo/RoyalCode.SmartSelector.Demo.csproj` — **25/25 aprovados**, incluindo consultas EF Core SQLite.
- `dotnet build RoyalCode.SmartSelector.Benchmarks/RoyalCode.SmartSelector.Benchmarks.csproj -c Release` — **0 erros, 0 warnings**.

---

## Fase 4 - Bugs de geração de baixo risco

**Depende de:** DF1, DF13, Fase 1.

**Escopo:** `AutoSelectGenerator.cs`, `AutoPropertiesGenerator.cs`, geradores de emissão; golden tests.

**O que/como:** corrigir B1, B3, B4 e B5 com um teste novo por bug (usar os cenários do harness de 2026-07-11 como base).

**Tarefas:**

- [x] B1: aplicar DF1 ao hintName de todos os arquivos gerados — `{Namespace}.{ContainingTypes}.{Classe}{Aridade}.{Categoria}.g.cs` com categorias `AutoSelect`, `Extensions`, `AutoProperties`, `AutoDetails`; testes com dois DTOs homônimos em namespaces distintos (a parte de containing types só é exercitável após a Fase 13, mas o formato já fica pronto).
- [x] B3: ampliar exclusão de propriedades declaradas para qualquer propriedade declarada no DTO (remover condição `SetMethod is not null` da exclusão); teste com propriedade get-only homônima.
- [x] B4: emitir `using System;`, `using System.Linq;`, `using System.Collections.Generic;` (ou nomes `global::`-qualificados) em todos os arquivos gerados; teste compilando sem implicit usings.
- [x] B5: detectar semanticamente membro homônimo **acessível** na cadeia de bases do DTO e emitir `new` apenas em `Select{TFrom}Expression` e `From` (nunca no campo privado — CS0109); testes: DTO herdando DTO (com `new`) e DTO sem conflito (sem `new`).

**Critérios de aceite:** os 4 cenários compilam sem CS8785/CS0102/CS0246/CS0108 e sem introduzir CS0109; snapshots existentes atualizados de forma revisável (diff contém apenas usings/`new` esperados).

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 4

**Concluída em 2026-07-11.**

- Os quatro emissores agora compartilham uma convenção de hint name com identidade completa e categoria (`AutoSelect`, `Extensions`, `AutoProperties`, `AutoDetails`); DTOs homônimos em namespaces diferentes não colidem mais. A cadeia de containing types e a aridade já entram no formato, embora o suporte de emissão para DTO aninhado continue reservado à Fase 13.
- `AutoProperties` exclui toda propriedade pública já presente no DTO, inclusive get-only, eliminando a duplicação CS0102.
- Todos os arquivos gerados declaram explicitamente `System`, `System.Linq` e `System.Collections.Generic`; o harness ganhou a opção de remover os global usings sintéticos e comprova a compilação nesse modo.
- A emissão de `new` considera membros acessíveis da cadeia de bases e também membros que serão gerados para uma base com `AutoSelect`; o modificador é aplicado somente à expressão pública e a `From`, nunca ao campo privado.
- Adicionados cinco testes em `GenerationHardeningTests`: colisão de hint names, get-only, compilação sem implicit usings, herança com conflito e ausência de falso positivo sem herança.
- Golden tests foram atualizados somente com os três usings explícitos; as referências aos artefatos passaram aos novos hint names. O comportamento de `new` é protegido pelo teste dedicado.
- Gates: `dotnet build SmartSelector.sln --no-restore` — **0 erros, 16 warnings já mapeados para fases posteriores**; testes principais com `Category!=KnownLimitation` — **43/43 aprovados**; Demo — **25/25 aprovados**. Os três testes `KnownLimitation` permanecem vermelhos por decisão de DF9 e serão tratados nas Fases 6/13.

---

## Fase 5 - Diagnósticos completos e localizados

**Depende de:** DF7, DF9, Fase 1.

**Escopo:** `AnalyzerDiagnostics.cs`, `AutoPropertiesGenerator.cs`, `AutoDetailsGenerator.cs`, `AnalyzerReleases.Unshipped.md`.

**O que/como:** eliminar falhas silenciosas e mensagens incorretas; novos IDs a partir de RCSS006; converter limitações em comportamento verificável.

**Tarefas:**

- [x] Criar diagnóstico específico para "classe com AutoProperties<T> não é partial" (substituir uso indevido de RCSS005 em `AutoPropertiesGenerator.cs:45-51`).
- [x] Criar diagnóstico para `[AutoProperties]` sem `[AutoSelect<T>]` (hoje silencioso).
- [x] Criar diagnósticos temporários para DTO genérico e DTO aninhado (removidos/reduzidos na Fase 13).
- [x] Adicionar `Location` real ao diagnóstico de `AutoDetailsGenerator.cs:33` (apontar a propriedade).
- [x] Criar diagnóstico warning para ambiguidade de flattening (múltiplos caminhos com mesmo prefixo).
- [x] Criar diagnóstico (ou suporte) para DTO em namespace global — hoje gera `.g.cs` inválido (achado do spike da Fase 0).
- [x] Registrar todos os novos IDs em `AnalyzerReleases.Unshipped.md`.
- [x] Criar um teste de diagnóstico por ID novo (asserção de ID + location).
- [x] Para cada limitação `KnownLimitation`, criar teste verde que verifica o diagnóstico temporário correspondente; o teste de compilação vermelho permanece com trait até a fase que o resolve (DF9).

**Critérios de aceite:** todo cenário rejeitado emite ao menos um RCSS com location válida; nenhum caso do harness gera "0 diagnósticos + 0 código gerado"; cada limitação tem par (teste verde de diagnóstico + teste vermelho de compilação com trait).

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`.

### Resultado da Fase 5

**Concluída em 2026-07-11.**

- Criados RCSS006–RCSS012, todos com localização real: classe não-partial com `AutoProperties<T>`, `[AutoProperties]` órfão, DTO genérico, DTO aninhado, flattening ambíguo, DTO no namespace global e sintaxe qualificada/aliased de `AutoProperties<T>` ainda não suportada.
- RCSS010 é warning e preserva a geração determinística atual; a detecção conta caminhos navegáveis concorrentes para a propriedade declarada. Os demais IDs novos são erros e interrompem somente o cenário rejeitado.
- O pipeline agora observa também o atributo `AutoPropertiesAttribute` não genérico para diagnosticar o uso sem `AutoSelect<T>`, sem duplicar geração nos usos válidos.
- O RCSS001 produzido por `AutoDetails` deixou de usar `Location.None` e aponta para o identificador da propriedade problemática.
- `AutoSelectInformation` pode transportar warnings junto com um resultado válido, permitindo diagnosticar ambiguidade sem suprimir os arquivos gerados.
- `GeneratorDiagnosticTests` contém um teste por ID novo, sempre verificando ID e texto exato da location, mais a regressão de location do RCSS001. RCSS008, RCSS009 e RCSS012 formam os pares verdes dos três testes de compilação `KnownLimitation`.
- Todos os IDs foram registrados em `AnalyzerReleases.Unshipped.md`.
- Gates: testes principais com `Category!=KnownLimitation` — **51/51 aprovados**; Demo — **25/25 aprovados**; solution build — **0 erros**. A execução informativa de `Category=KnownLimitation` permanece com **3/3 falhando por design**, agora exibindo respectivamente RCSS008, RCSS009 e RCSS012.

---

## Fase 6 - AutoProperties<T> semântico

**Depende de:** DF3, Fase 1.

**Escopo:** `AutoPropertiesGenerator.Transform`, `MapFromPropertyNameResolver` (avaliar remoção do fallback sintático).

**O que/como:** resolver TFrom, `Exclude` e `Flattening` via `context.Attributes`/`AttributeData`; sintaxe apenas para location de diagnóstico.

**Tarefas:**

- [x] Reescrever `Transform` para usar `context.Attributes[0]` (TFrom via `AttributeClass.TypeArguments`, named args via `NamedArguments`).
- [x] Remover a busca sintática por `IdentifierNameSyntax`/`GenericNameSyntax` (`AutoPropertiesGenerator.cs:54-57`), mantendo detecção de conflito genérico/não-genérico por símbolo.
- [x] Avaliar e, se possível, remover o fallback de inspeção sintática de `MapFromPropertyNameResolver` (código morto se `ConstructorArguments` sempre materializa).
- [x] Tornar verde o teste TDD `Generated_code_should_compile_with_a_fully_qualified_AutoProperties_attribute` e remover seu trait `KnownLimitation`.

**Critérios de aceite:** `[global::RoyalCode.SmartSelector.AutoProperties<X>]` e aliases geram o mesmo código da forma simples; teste TDD correspondente verde e no gate padrão.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"` (gate); execução informativa `--filter "Category=KnownLimitation"` deve mostrar exatamente 2 limitações restantes (baseline DF9).

### Resultado da Fase 6

**Concluída em 2026-07-12.**

- `AutoPropertiesGenerator.Transform` agora obtém o atributo tipado por `context.Attributes`, resolve `TFrom` por `AttributeClass.TypeArguments` e lê `Exclude`/`Flattening` exclusivamente de `AttributeData.NamedArguments`; a sintaxe do atributo ficou restrita à location de RCSS005.
- A detecção de conflito entre `AutoPropertiesAttribute` e `AutoPropertiesAttribute<TFrom>` passou a comparar símbolos/metadados, funcionando também com nomes qualificados e aliases.
- Foram removidas a busca por `IdentifierNameSyntax`/`GenericNameSyntax` e a criação de `TypeDescriptor` a partir do argumento sintático.
- O tratamento de arrays nomeados ignora corretamente valores `null` antes de enumerar `TypedConstant.Values`.
- O fallback sintático de `MapFromPropertyNameResolver` foi removido. Literais, `nameof`, forma qualificada e alias são resolvidos pelo argumento constante do construtor em `AttributeData`; o teste por alias protege essa decisão.
- `AutoPropertiesSemanticTests` compara os arquivos gerados pelas formas simples, `global::` e alias, incluindo `Exclude` e `Flattening`, e verifica `MapFrom` por alias.
- O teste TDD de `AutoProperties<T>` qualificado ficou verde e saiu de `KnownLimitation`. RCSS012, criado como diagnóstico estritamente temporário na Fase 5, foi removido de código, testes e `AnalyzerReleases.Unshipped.md` antes de ser publicado.
- Nenhum golden snapshot precisou ser alterado.
- Gates: testes principais com `Category!=KnownLimitation` — **53/53 aprovados**; execução informativa `Category=KnownLimitation` — **exatamente 2/2 falhando por design** (RCSS008 e RCSS009); Demo — **25/25 aprovados**; solution build — **0 erros, 0 warnings**; `git diff --check` aprovado.

---

## Fase 7 - Refactors internos do generator

**Depende de:** Fase 6 (evita retrabalho na consolidação de `CreateInformation`); DF16/DF17 para as tarefas de API pública.

**Escopo:** `RoyalCode.SmartSelector.Generators/**`, `RoyalCode.SmartSelector/*.cs`.

**O que/como:** eliminar duplicação sem alterar o texto gerado (golden tests devem passar sem edição).

**Tarefas:**

- [x] Extrair helper único para mapear `DeclaredAccessibility` → modificadores (hoje triplicado em `AutoSelectGenerator`, `AutoPropertiesGenerator`, `AutoDetailsGenerator`), com fallback definido e tratamento de `ProtectedOrInternal`/`ProtectedAndInternal`.
- [x] Extrair `SequenceEqual`/`SequenceHashCode` duplicados das 3 classes `*Information` para utilitário interno.
- [x] Consolidar os overloads restantes de `CreateInformation` em `AutoPropertiesGenerator` (pós-Fase 6).
- [x] Unificar os `Predicate` idênticos de `AutoSelectGenerator` e `AutoPropertiesGenerator`.
- [x] Padronizar idioma dos comentários conforme DF8.
- [x] Selar os atributos públicos (DF16).
- [x] Extrair classe base pública com `Exclude`/`Flattening` para os atributos (DF17).
- [x] Adicionar `Microsoft.CodeAnalysis.PublicApiAnalyzers` ao projeto runtime com snapshot da API pública (`PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`), registrando as mudanças de DF16/DF17.
- [x] Criar teste de compilação de consumidor que deriva dos atributos, documentando que agora falha (efeito esperado de DF16).
- [x] Registrar a versão NuGet alvo do breaking (0.5.0), nota de release/migração e a decisão de `AssemblyVersion` em `Directory.Build.props`.

**Critérios de aceite:** todos os golden tests passam sem nenhuma alteração de string esperada; nenhuma duplicação das três estruturas citadas (verificável por grep); atributos selados e herdando da base compartilhada; snapshot de API pública commitado e refletindo DF16/DF17.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 7

**Concluída em 2026-07-12.**

- `GeneratedSourceConventions.ApplyDeclaredAccessibility` centraliza o mapeamento de acessibilidade com fallback público e cobre `protected internal`/`private protected`; os três emissores passaram a usá-lo.
- `InformationEquality` concentra `SequenceEqual`/`SequenceHashCode` das três classes `*Information`, e `GeneratorSyntaxPredicates.IsClass` substitui os predicates duplicados dos pipelines.
- `AutoPropertiesGenerator` expõe apenas um overload de `CreateInformation`; a implementação com coleções foi renomeada para core privado. O caminho não genérico de `AutoSelect` também passou a resolver `AutoProperties` semanticamente.
- Comentários internos alterados foram padronizados em português conforme DF8; identificadores, diagnósticos e XML docs públicos permanecem em inglês.
- `AutoSelectAttribute<TFrom>`, `AutoPropertiesAttribute`, `AutoPropertiesAttribute<TFrom>` e `AutoDetailsAttribute` agora são selados. A nova base pública abstrata `AutoPropertiesAttributeBase` declara `Exclude` e `Flattening` para as duas formas de `AutoProperties` e para `AutoDetails`.
- `PublicAttributeContractTests` foi criado primeiro em vermelho e agora comprova quatro erros CS0509 para consumidores que tentem herdar dos atributos selados.
- `Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 protege o runtime. `PublicAPI.Shipped.txt` registra a API 0.4.x e `PublicAPI.Unshipped.txt` registra a nova base e as remoções binárias dos membros cujo declaring type mudou; build isolado do runtime: **0 warnings RS**.
- A versão alvo passou a **0.5.0**. Durante a série 0.x, `AssemblyVersion` acompanha `Ver`, resultando em **0.5.0.0**. `RELEASE_NOTES.md` documenta breaking changes, recompilação, reflexão com `DeclaredOnly` e migração; o arquivo está incluído no `.nupkg`.
- Nenhum golden snapshot foi alterado. As buscas de verificação não encontram predicates duplicados, helpers de sequência duplicados nem comparações locais de `DeclaredAccessibility`; há um único overload público interno chamado `CreateInformation`.
- Gates: testes principais com `Category!=KnownLimitation` — **54/54 aprovados**; `KnownLimitation` — **exatamente 2/2 falhando por design**; Demo — **25/25 aprovados**; solution build — **0 erros, 16 warnings já mapeados para fases posteriores**; runtime/API build — **0 erros, 0 warnings**; `git diff --check` aprovado.

---

## Fase 8 - Empacotamento e dependências

**Depende de:** Fase 0 (validação de DF15) e `RoyalCode.Extensions.SourceGenerator` 0.1.14.

**Escopo:** `RoyalCode.SmartSelector.Generators.csproj`, `RoyalCode.SmartSelector.Demo.csproj`, `RoyalCode.SmartSelector.Benchmarks.csproj`, `tests.targets`, `README.md`.

**O que/como:** remover caminhos hardcoded de empacotamento e aplicar o multi-target Roslyn de DF15 validado na Fase 0.

**Tarefas:**

- [x] Trocar `$(NuGetPackageRoot)royalcode.extensions.sourcegenerator\...` por `GeneratePathProperty="true"` + `$(PKGRoyalCode_Extensions_SourceGenerator)` nos 3 csproj que usam o caminho.
- [x] Reavaliar `NoWarn` de `NU1900` no generator; remover se o restore não o exigir mais.
- [x] No pacote externo 0.1.14, baixar `Microsoft.CodeAnalysis.CSharp` para 4.8.0 e usar uma versão compatível de `Microsoft.CodeAnalysis.Analyzers`, corrigir os contratos de equality/hash de `TypeDescriptor`, `PropertyDescriptor` e `MatchSelection`, e eliminar os caches estáticos de descritores que retêm símbolos; cobrir as correções com testes antes do release.
- [x] Aplicar DF15: multi-target do generator por versão de Roslyn (`analyzers/dotnet/roslyn4.8/cs` + `analyzers/dotnet/roslyn5.6/cs`) conforme validado na Fase 0; testes podem permanecer em versão mais nova.
- [x] Inspecionar o `.nupkg` gerado (conteúdo das pastas `analyzers/dotnet/...`, incluindo o `RoyalCode.Extensions.SourceGenerator.dll` em cada pasta roslyn quando multi-target).
- [x] Documentar no README o SDK mínimo exigido por variante, citando a matriz da Fase 0.

**Critérios de aceite:** externo 0.1.14 publicado com testes de equality/hash e sem caches estáticos que retenham símbolos; `dotnet pack` do generator produz `.nupkg` com layout multi-target correto; projeto de consumo em SDK 8.0.x compila com o generator ativo (verificado com o `.nupkg`); build limpo após remoção do caminho hardcoded.

**Testes:** `dotnet pack RoyalCode.SmartSelector.Generators\RoyalCode.SmartSelector.Generators.csproj -c Release`; inspecionar o `.nupkg`; `dotnet test` das duas suítes com `--filter "Category!=KnownLimitation"` no projeto Tests.

### Resultado da Fase 8

**Concluída em 2026-07-12.**

- O pacote externo foi atualizado para `RoyalCode.Extensions.SourceGenerator` 0.1.14 no repositório `RoyalCode/Utils`, commit `b141485` (`fix: harden source generator descriptor contracts`). `Microsoft.CodeAnalysis.CSharp` foi reduzido para 4.8.0; `Microsoft.CodeAnalysis.Analyzers` ficou em 3.3.4 porque não existe versão 4.8.0 desse pacote.
- Os contratos de igualdade/hash de `TypeDescriptor`, `PropertyDescriptor` e `MatchSelection` foram corrigidos. Os caches estáticos de `CancellationToken` e `void` que podiam reter símbolos/compilações foram removidos. A suíte externa passou com 30/30 testes; build Release com 0 avisos e 0 erros.
- O `.nupkg` externo 0.1.14 foi publicado no NuGet.org com metadado de repositório apontando para o commit `b141485`. Um restore limpo, sem feed local, baixou o pacote público; uma validação direta do DLL confirmou os contratos de equality/hash e a ausência dos caches estáticos que retinham símbolos.
- Os caminhos baseados em `$(NuGetPackageRoot)` foram substituídos por `GeneratePathProperty`; `NU1900` foi removido de `NoWarn` sem reaparecer no restore.
- O generator passou a produzir variantes Roslyn 4.8 e 5.6. O pacote `RoyalCode.SmartSelector.Generators.0.5.0.nupkg` contém o generator e `RoyalCode.Extensions.SourceGenerator.dll` em `analyzers/dotnet/roslyn4.8/cs` e `analyzers/dotnet/roslyn5.6/cs`; as duas cópias da auxiliar correspondem ao binário 0.1.14 testado.
- A matriz real de consumo do `.nupkg` passou: SDK 8.0.422 e 9.0.100 carregaram `roslyn4.8`; SDK 10.0.301 carregou `roslyn5.6`; todos compilaram e executaram com o generator ativo.
- Validação do repositório: solução compilada sem erros (16 avisos preexistentes nos modelos esperados), suíte principal 54/54, Demo 25/25 e Benchmarks Release com 0 avisos/0 erros.

O restore da solução, o empacotamento e toda a matriz foram repetidos usando o pacote público. As duas variantes do `.nupkg` do generator incorporaram exatamente o DLL público 0.1.14. Todos os critérios de aceite da Fase 8 foram atendidos.

---

## Fase 9 - CI e release com gates

**Depende de:** Fase 1 (traits); DF19 para o job de release.

**Escopo:** `.github/workflows/*`.

**O que/como:** separar CI de release (revisão item 7, opção A) com release manual aprovado (DF19) e corrigir nomes herdados de SmartSearch.

**Tarefas:**

- [x] Criar/ajustar workflow de CI: build + `dotnet test --filter "Category!=KnownLimitation"` + `dotnet pack` em PR e push para main.
- [x] Adicionar ao CI um job informativo que executa os testes `KnownLimitation` e valida a baseline nominal (DF9): falha se a quantidade aumentar, se aparecer nome fora da baseline, ou se um teste da baseline passar (deve ser removido da lista); a falha esperada dos testes conhecidos não bloqueia. Publica nomes e contagem no summary.
- [x] Criar teste de consumo: instalar os `.nupkg` produzidos em projeto temporário e compilar uma projeção mínima.
- [x] Corrigir nomes de etapas que referenciam SmartSearch.
- [x] Criar workflow de release `workflow_dispatch` com aprovação (DF19) e prova de procedência: input obrigatório `ci_run_id`; o workflow valida conclusão `success`, branch (`main` ou tag autorizada) e commit SHA do run; baixa os artefatos pelo ID **sem rebuild**; exibe SHA e versões no environment de aprovação; publica exatamente os arquivos baixados (sem glob amplo).
- [x] ~~Após o push, configurar o environment `nuget-production` com reviewer obrigatório, configurar os jobs do CI como checks obrigatórios de `main` e executar os testes de aceite no GitHub Actions.~~ *(Sem efeito — release gated revertido por decisão do humano em 2026-07-12; ver Resultado da Fase 9.)*

**Critérios de aceite:** PR com teste falhando bloqueia merge; summary do CI mostra nomes e contagem da baseline `KnownLimitation`; teste de consumo compila usando exclusivamente os `.nupkg`; release recusa `ci_run_id` de run falho, de branch não autorizada ou com artefato ausente, e não faz rebuild.

**Testes:** execução dos workflows em PR de teste; `gh run watch` verde.

### Resultado da Fase 9

**Concluída em 2026-07-12** — com escopo de release revisto pelo humano (ver atualização ao final desta seção).

- O workflow manual legado `smart-select.yml`, que publicava por glob e ainda citava SmartSearch, foi substituído por `ci.yml` e `release.yml`.
- O CI executa em PR, push para `main`, tags `v*` e disparo manual. Ele restaura e compila a solução em Release, executa o gate `Category!=KnownLimitation`, testa o Demo, empacota os dois projetos e publica um único artefato imutável com os dois `.nupkg` e um manifesto com commit e SHA-256.
- O smoke test cria um consumidor temporário sob SDK 8, usa uma fonte NuGet contendo exclusivamente os pacotes produzidos e compila/executa uma projeção gerada. A reprodução local confirmou SDK 8.0.422 e saída `Package consumer OK`; os metadados do cache apontaram somente para o diretório local de artefatos.
- O job `Known limitations baseline` executa a suíte informativa e exige nominalmente os dois testes restantes. Ele falha em caso de teste inesperado, ausente, aprovado ou com outcome diferente de `Failed`, e publica nomes, outcomes e contagem no summary. A baseline local foi exatamente 2/2 falhas esperadas.
- O release exige `ci_run_id`, consulta a API do GitHub e recusa run que não pertença a `ci.yml`, não esteja concluído com `success`, não corresponda ao SHA atual de `main` ou a uma tag semântica autorizada, ou não contenha o artefato/manifesto exato. Antes e depois do gate ele valida nomes e SHA-256; não há checkout, build ou pack, e cada arquivo aprovado é publicado por caminho explícito.
- Validação local: `yaml-lint` aprovado; `actionlint` 1.7.12 aprovado; build Release da solução com 0 erros/0 avisos; dois pacotes 0.5.0 inspecionados; consumidor exclusivo aprovado; baseline nominal aprovada.

**Atualização 2026-07-12 (decisão do humano):** o workflow `release.yml` (release gated por `ci_run_id`, DF19) foi removido e o workflow manual legado `smart-select.yml` foi restaurado — `workflow_dispatch` fazendo build, pack e `dotnet nuget push` por glob (nomes de etapas corrigidos para SmartSelector; `--skip-duplicate` adicionado). O `ci.yml` permanece ativo e funcional. As tarefas de release com procedência e a tarefa pendente de configurar o environment `nuget-production` ficam sem efeito até nova decisão sobre DF19.

---

## Fase 10 - Contrato do AutoDetails

**Depende de:** DF2, Fases 0 e 5.

**Escopo:** `AutoDetailsGenerator.cs`, testes de AutoDetails.

**O que/como:** o tipo declarado na propriedade é a fonte de verdade (revisão item 2, opção A).

**Tarefas:**

- [x] Usar nome/namespace de `property.Type` para a classe gerada (corrige B2).
- [x] Tratar tipo já existente: gerar parte apenas se `partial`; diagnóstico caso contrário.
- [x] Diagnóstico para geração duplicada (duas propriedades `[AutoDetails]` para o mesmo tipo) e para acessibilidade incompatível.
- [x] Remover a mutação `propertyType.Namespaces[0] = ...` (`AutoDetailsGenerator.cs:47`) construindo descriptor novo.
- [x] Testes: tipo com nome fora da convenção (`AddressDto`), tipo pré-existente partial, duplicidade.

**Critérios de aceite:** cenário `AddressDto` do harness compila; nenhum caso de AutoDetails gera classe com nome diferente do declarado.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj --filter "Category!=KnownLimitation"`; `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj`.

### Resultado da Fase 10

**Concluída em 2026-07-12.**

- DF2 aplicada: a classe gerada pelo AutoDetails usa o nome do tipo declarado na propriedade (`property.Type`), eliminando B2; o namespace vem do tipo existente quando ele já está declarado, ou do tipo que declara a propriedade quando o tipo é novo. O cenário `AddressDto` (nome fora da convenção) compila nos três TFMs.
- Tipo preexistente é completado somente quando é uma classe `partial` declarada na própria compilação; caso contrário é emitido **RCSS012** (ID reutilizado — o RCSS012 da Fase 5 foi removido na Fase 6 antes de qualquer publicação). Propriedades já declaradas pelo usuário no tipo preexistente não são geradas novamente.
- Tipos `partial` preexistentes no namespace global são emitidos sem declaração `namespace`; o caminho dedicado preserva cabeçalho, nulabilidade, XML docs, `[GeneratedCode]` nos membros e acessibilidade.
- **RCSS013** cobre duas propriedades `[AutoDetails]` que gerariam o mesmo tipo (detecção por chave namespace+nome dentro do DTO); **RCSS014** cobre tipo existente menos acessível que a exposição efetiva da propriedade. Os três IDs foram registrados em `AnalyzerReleases.Unshipped.md`.
- A mutação `propertyType.Namespaces[0] = ...` foi removida: um `TypeDescriptor` novo é construído para o tipo gerado; `DefinedProperties` é definido no descriptor novo e no da propriedade (necessário para a correspondência do `AutoDetailsAssignDescriptorResolver`).
- Correção de fluxo necessária para os novos diagnósticos: os branches de falha do match em `AutoSelectGenerator.Transform` descartavam o `AutoPropertiesInformation` e, com ele, os diagnósticos de AutoDetails — o usuário via apenas um RCSS002 confuso. Agora os diagnósticos de AutoDetails prevalecem e as falhas de match das mesmas propriedades são suprimidas; `AutoDetailsInformation` expõe `PropertyName`/`Diagnostics`, incluídos na igualdade do modelo.
- `AutoDetailsContractTests` cobre os cinco cenários: tipo declarado fora da convenção (compilação nos 3 TFMs + ausência de `AddressDetails`), tipo preexistente partial (parte gerada sem regenerar propriedade do usuário), RCSS012, RCSS013 e RCSS014 — sempre verificando ID e texto exato da location.
- Nenhum golden snapshot foi alterado; o golden existente de AutoDetails (`AddressDetails` por convenção) permaneceu idêntico.
- Gates: testes principais com `Category!=KnownLimitation` — **59/59 aprovados**; `KnownLimitation` — **exatamente 2/2 falhando por design**; Demo — **25/25 aprovados**; solution build — **0 erros**; `git diff --check` aprovado.

---

## Fase 11 - Código gerado auto-suficiente e nullable-clean

**Depende de:** DF6, DF11, Fases 0, 1 e 4; `RoyalCode.Extensions.SourceGenerator` 0.1.15 para nulabilidade.

**Escopo:** emissores (`ClassGenerator` uso, `AutoSelectGenerator`, `AutoPropertiesGenerator`, `AutoDetailsGenerator`); todos os snapshots; `RoyalCode.Extensions.SourceGenerator` 0.1.15 para `NullableAnnotation`.

**O que/como:** cabeçalho, metadados e nulabilidade correta no código emitido, com gradação de `[GeneratedCode]` (DF11).

**Tarefas:**

- [x] Emitir `// <auto-generated/>` no topo de todo arquivo gerado.
- [x] Emitir `[GeneratedCode("RoyalCode.SmartSelector.Generators", "<versão>")]` no tipo apenas para classes sem nenhuma declaração do usuário (`{Dto}_Extensions`, AutoDetails gerado do zero) — DF11.
- [x] Emitir `[GeneratedCode]` nos **membros** gerados quando existe declaração do usuário (DTOs parciais e AutoDetails completando tipo `partial` preexistente) — DF11.
- [x] Validar o efeito com analyzers e cobertura (arquivo `.g.cs` do Demo) antes de fechar a gradação.
- [x] Emitir XML docs (`/// <summary>`) nos membros públicos gerados (elimina CS1591 em consumidores com docs obrigatórias).
- [x] Declarar o cache como `private static Func<TFrom, TDto>? select{X}Func;`.
- [x] Adicionar e consumir `NullableAnnotation` em `TypeDescriptor` no pacote externo 0.1.15; preservar as anotações nullable nos tipos das propriedades geradas sem um modelo local paralelo.
- [x] Atualizar todos os golden tests em commit separado das mudanças de emissor (diff revisável).

**Critérios de aceite:** snapshots compilados como fonte comum com `nullable enable` não emitem CS8618 pelo campo de cache; arquivos gerados começam com `// <auto-generated/>`; declaração partial de DTO **não** carrega `[GeneratedCode]` no tipo; consumidor com `GenerateDocumentationFile=true` não recebe CS1591 de membros gerados.

**Testes:** `dotnet test` das duas suítes com `--filter "Category!=KnownLimitation"` no projeto Tests + verificação manual de um `.g.cs` do Demo (`EmitCompilerGeneratedFiles`).

### Resultado da Fase 11

**Concluída em 2026-07-12.**

- **Externo 0.1.15:** `TypeDescriptor` ganhou a propriedade `NullableAnnotation` (dado equatable, incluído em `Equals`/`GetHashCode`), populada em `Create(ITypeSymbol)` a partir do símbolo e derivada do texto na forma sintática. O `Name` não foi alterado para não interferir no matching por nome. Suíte externa 34/34; pacote validado embutido nas duas variantes do `.nupkg` do generator. **Pendência: publicar 0.1.15 no NuGet.org antes do push** (o restore do CI falhará sem ele; validação local usou o cache global).
- **Emissão:** todo arquivo gerado inicia com `// <auto-generated/>` + `#nullable enable annotations` — as anotações valem para consumidores, mas o contexto de warnings fica desabilitado dentro do arquivo gerado (sem CS8618/CS8601 em código que o usuário não pode corrigir).
- **DF11 aplicada:** `[global::System.CodeDom.Compiler.GeneratedCode(tool, versão-do-assembly)]` no tipo para `{Dto}_Extensions` e AutoDetails gerado do zero; nos membros para DTOs parciais e AutoDetails completando partial preexistente. Nota: a versão embutida ("0.5.0.0") fará os goldens mudarem a cada bump de versão.
- **XML docs** de linha única em todos os membros públicos gerados; teste dedicado compila com `DocumentationMode.Diagnose` e comprova zero CS1591 originado de `.g.cs`.
- **Nullable-clean:** campo de cache emitido como `Func<TFrom, TDto>?`; anotações nullable de referência preservadas nas propriedades geradas via `NullableAnnotation` (helper `PreserveNullableAnnotation`, sem modelo local paralelo). Propriedades `string?` da origem, antes silenciosamente descartadas por `IsSupportedType`, agora são geradas (`UnderlyingType` decide o suporte).
- **Bug corrigido descoberto pelos gates:** `AutoPropertyOriginPropertiesRetriever` passava o `AutoSelectAttribute` (sem `Exclude`/`Flattening`) ao `CreateInformation`; o `Exclude` era ignorado no match e só não quebrava porque `string?` era tipo não suportado. Com a correção, `Exclude = [nameof(Book.Sku)]` do Demo volta a valer no match.
- **Suportes locais de emissão** (pacote externo sem atributos/docs em propriedades/campos, como mapeado no spike): `RawLinesGeneratorNode`, `AnnotatedPropertyGenerator` e `AnnotatedFieldGenerator` em `GeneratedMemberNodes.cs`.
- **Golden tests:** todos os snapshots atualizados mecanicamente por script (header, campo nullable, docs e atributos); transformação puramente aditiva conferida por diff (~330 inserções; únicas linhas alteradas são as dos campos de cache). Espelhos `Models/Expected` sincronizados: campos de cache nullable nos arquivos com nullable habilitado e `new` em `BlogsPosts.cs` (elimina CS0108); arquivos sob `#nullable disable` mantêm o campo sem `?` (evita CS8632). Justificativa da invariante 4 registrada aqui; recomenda-se commitar emissor e snapshots em commits separados.
- **Demo:** `.g.cs` emitido em disco verificado com o novo formato; 20 arquivos gerados obsoletos com hint names pré-Fase 4 (ex.: `BookDetails.g.cs`) removidos do diretório `Generated/`.
- **Docs:** exemplos de código gerado em `docs.md` atualizados (campo nullable); `RELEASE_NOTES.md` ganhou a seção "Generated code changes" (forma observável por consumidores, incluindo as mudanças da Fase 10).
- **Novos testes:** `GeneratedCodeConventionsTests` (5): header em todos os arquivos, gradação DF11 (tipo × membro), campo nullable, preservação de `string?`/`Int32?` e ausência de CS1591.
- **Gates:** solution build — **0 erros, 12 warnings restantes** (somente CS8601/CS8604 em `Nulls.cs`, alvos da Fase 12; CS8618/CS0108/CS8632 zerados); testes principais — **64/64**; `KnownLimitation` — **exatamente 2/2 falhando por design**; Demo — **25/25**; Benchmarks Release — **0 erros**; pack Release multi-target íntegro com o externo 0.1.15.

---

## Fase 12 - Política de null em From e coleções

**Depende de:** DF5, DF18, Fase 11.

**Escopo:** `SelectLambdaGenerator.cs`, `MatchSelection`/`AssignDescriptor` (pacote externo, se necessário), `AnalyzerDiagnostics.cs`, testes novos de execução em memória e EF Core.

**O que/como:** implementar a opção A da revisão (condicionais explícitas na expression tree) com a política direcional de DF5 e o fallback de coleção vazia com diagnóstico Info de DF18.

**Tarefas:**

- [x] Navegação nullable → destino nullable: gerar `a.X == null ? null : ...` (objetos e flattening).
- [x] Escalar nullable → destino non-nullable: manter comportamento atual + diagnóstico warning (DF5).
- [x] Coleção nullable → destino non-nullable: gerar fallback de coleção vazia traduzível e emitir diagnóstico de severidade Info na propriedade (DF18); registrar o novo ID em `AnalyzerReleases.Unshipped.md`.
- [x] Testes de execução em memória (`From`, `Select` de `IEnumerable`) com grafos nulos — sem `NullReferenceException` nos casos cobertos pela política.
- [x] Testes EF Core SQLite no Demo garantindo tradutibilidade das novas condicionais.
- [x] Documentar a política em `docs.md` (nova seção).

**Critérios de aceite:** `From` com navegação nula não lança NRE quando destino é nullable; consultas do Demo continuam traduzindo (25/25 + novos casos verdes); política documentada.

**Testes:** `dotnet test` das duas suítes com `--filter "Category!=KnownLimitation"` no projeto Tests; novos testes nomeados `NullPolicy*`.

### Resultado da Fase 12

**Concluída em 2026-07-12.**

- **Política direcional implementada localmente**, sem mudanças no pacote externo: `NullAssignmentPolicy` (novo, em `Models/`) classifica cada `PropertyMatch` — a mesma classificação alimenta a forma da expressão no `SelectLambdaGenerator` (Generate) e os diagnósticos no `Transform` do AutoSelect. A política considera anotações nullable de referência e `Nullable<T>`; código oblivious mantém o comportamento anterior (todos os goldens `#nullable disable` inalterados).
- **DF5 aplicada:** navegação/objeto anulável → destino anulável gera `a.X == null ? null : new T {...}`; flattening por pai anulável → destino anulável gera `a.P == null ? null : a.P.X` (com `||` para múltiplos pais); escalar anulável → destino non-nullable mantém a atribuição direta e emite **RCSS015** (Warning) apontando a propriedade do DTO.
- **DF18 aplicada:** coleção anulável → destino non-nullable gera `a.Items == null ? new List<TDto>() : a.Items.Select(...).ToList()` e emite **RCSS016** (Info). Coleção anulável → destino anulável propaga null. IDs registrados em `AnalyzerReleases.Unshipped.md`.
- **Complemento após revisão:** escalares `Nullable<T>` projetados em destinos non-nullable também recebem RCSS015; flattening por pai nullable para destino `Nullable<T>` propaga null com `default(T?)`; diagnósticos de matches internos são percorridos recursivamente e ancorados na propriedade raiz com o caminho completo.
- **Correções que a política expôs:** (a) `AddressDetails?` em propriedade `[AutoDetails]` com tipo ainda não gerado é vinculado pelo compilador como `Nullable<ErrorType>` — o `TryCreate` agora desembrulha o argumento de tipo e usa `UnderlyingType` no nome/hintName; (b) a emissão de `new T`/argumento genérico em `SelectLambdaGenerator` usava `Name` cru e quebrava com nomes anotados (`new AddressDetails?`) — agora usa o tipo subjacente.
- **Achado registrado (fora de escopo, preexistente):** coleções com tipo concreto `List<T>` na entidade não são reconhecidas pelo resolver de enumeráveis do pacote externo e caem em `NewInstance` sobre as propriedades de `List` (gera código inválido com `Capacity`/`this[]`); as interfaces (`ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`) funcionam. Candidato a correção externa futura.
- **Testes:** `NullPolicyTests` (9) cobre propagação para navegação/coleção, fallback vazio com RCSS016 (ID+location+severidade), warning RCSS015 para referências e `Nullable<T>`, flattening nullable para value type, matches internos, comportamento oblivious inalterado e **execução em memória real** (emit + reflection): `From` com grafo nulo não lança NRE e produz coleção vazia. `Util.CompileResult` passou a expor `OutputCompilation` para os testes de execução.
- **EF Core:** novo cenário Store no Demo (`Supplier`/`Warehouse?`/`ICollection<Contact>?` → `SupplierDetails` com `WarehouseDetails?` e `IReadOnlyList<ContactDetails>` non-nullable). `StoreDemoTests` comprova a **tradutibilidade das duas condicionais no SQLite** e a semântica em memória. Golden atualizado: apenas `NullableAndCastSelectorTests10` (fixture nullable-aware; Value3 ganhou fallback vazio e Value4 propagação de null).
- **Docs:** nova seção "10. Política de Null" em `docs.md` com a tabela direcional completa; `RELEASE_NOTES.md` atualizado.
- **Gates após o complemento:** solution build — **0 erros, 8 warnings** (6 CS8601/CS8604 nos espelhos manuais de `Nulls.cs` e 2 RCSS015 agora corretamente expostos no Demo); testes principais — **74/74**; `KnownLimitation` — **exatamente 2/2 falhando por design**; Demo — **28/28** (incluindo os 3 novos de Store); Benchmarks Release — **0 erros/0 warnings**.

---

## Fase 13 - DTOs aninhados e diagnóstico permanente para genéricos

**Depende de:** DF7, DF20, Fases 0 e 5; `RoyalCode.Extensions.SourceGenerator` 0.1.16 com containing types.

**Escopo:** emissão de declarações parciais (`ClassGenerator` — mudança no pacote externo conforme spike, com release NuGet), `AutoSelectGenerator`, `AutoPropertiesGenerator`.

**O que/como:** gerar a cadeia completa de declarações contendo (revisão item 4, opção A) para tipos aninhados; DTOs genéricos rejeitados permanentemente com diagnóstico (DF20).

**Tarefas:**

- [x] Emitir cadeia de tipos contenedores (`partial class Container { partial class EntityDetails { ... } }`) com modificadores corretos.
- [x] Tornar verde `Generated_code_should_compile_for_a_nested_destination_dto` e remover trait.
- [x] Tornar permanente o diagnóstico de DTO genérico criado na Fase 5 e converter `Generated_code_should_compile_for_a_generic_destination_dto` em teste de diagnóstico verde (DF20).
- [x] Emitir o mesmo diagnóstico permanente quando qualquer containing type do DTO aninhado for genérico (`Container<T>.EntityDetails`), com teste de diagnóstico (DF20).
- [x] Remover o diagnóstico temporário de DTO aninhado da Fase 5.
- [x] Exercitar o hintName de DF1 com containing types: dois DTOs aninhados homônimos em containing types distintos geram arquivos distintos.

**Critérios de aceite:** zero testes com trait `KnownLimitation` restantes; DTO genérico e containing type genérico produzem diagnóstico permanente com location no identificador da classe; aninhados homônimos não colidem em hintName. A partir desta fase, a suíte sem filtro é o gate padrão.

**Testes:** `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj` (sem filtro) verde.

### Resultado da Fase 13

**Concluída em 2026-07-12.**

- `RoyalCode.Extensions.SourceGenerator` 0.1.16 adicionou `ContainingTypeGenerator` e suporte no `ClassGenerator` para emitir uma cadeia ordenada de declarações contenedoras. O pacote foi publicado e o restore foi confirmado em cache NuGet vazio usando somente fontes configuradas, antes do fechamento da fase.
- `AutoSelect` e `AutoProperties<T>` reaplicam a cadeia completa de tipos, preservando tipo de declaração (`class`, `struct` ou `record`), acessibilidade e `partial`. Um containing type não-`partial` produz diagnóstico localizado em vez de código C# inválido.
- As extension classes permanecem no namespace, conforme exigido pelo C#, e usam o nome qualificado do DTO (`Container.EntityDetails`) e identificador com a cadeia (`Container_EntityDetails_Extensions`), evitando colisões entre DTOs aninhados homônimos.
- RCSS008 tornou-se o diagnóstico permanente para DTO genérico e para qualquer containing type genérico, sempre localizado no identificador do DTO. RCSS009, temporário para aninhados, foi removido antes da publicação do analyzer.
- A baseline `KnownLimitation` foi zerada. `GeneratedCodeCompilationTddTests` agora valida `AutoSelect` e `AutoProperties<T>` aninhados nos reference assemblies net8/net9/net10; `GenerationHardeningTests` cobre hint names e tipos de extensão distintos para aninhados homônimos.
- Gates: restore público em cache vazio; solution build — **0 erros, 8 warnings já esperados da política de null**; suíte principal sem filtro — **78/78**; Demo — **28/28**; Benchmarks Release — **0 erros/0 warnings**.

---

## Fase 14 - Pipeline incremental sem retenção de símbolos

**Depende de:** Fase 0 (mapeamento de retenção), Fases 4–13 (evitar refazer trabalho sobre modelos que ainda vão mudar) e release externo 0.2.0 coordenado.

**Escopo:** `AutoSelectInformation`, `AutoPropertiesInformation`, `AutoDetailsInformation`, `Transform`s, `SelectLambdaGenerator`; `RoyalCode.Extensions.SourceGenerator` (release coordenado conforme spike).

**O que/como:** extrair no Transform todos os dados necessários para strings/estruturas equatable; `Diagnostic` real criado apenas em `RegisterSourceOutput` a partir de `DiagnosticInfo` serializável; geração 100% pura.

**Tarefas:**

- [x] Definir `DiagnosticInfo` (id, args, FilePath+TextSpan+LineSpan) e substituir `Diagnostic[]` nos modelos.
- [x] Remover `ISymbol` dos dados retidos pelos modelos (consumir `TypeDescriptor.Symbol` dentro do Transform e descartar).
- [x] Eliminar mutações durante a geração (`AddParentProperty` em `SelectLambdaGenerator.cs:109` → cálculo de caminho imutável).
- [x] Adicionar teste de cacheabilidade usando `GeneratorDriver` com `trackIncrementalGeneratorSteps: true` verificando `IncrementalStepRunReason.Cached`/`Unchanged` após edição irrelevante.
- [x] Adicionar teste de retenção transitiva: percorrer por reflexão o grafo de objetos alcançável a partir dos modelos cacheados e falhar se qualquer nó for `ISymbol`, `SyntaxNode`, `SyntaxTree`, `SemanticModel`, `Compilation`, `Diagnostic` ou `Location`.
- [x] Aplicar o `RoyalCode.Extensions.SourceGenerator` 0.2.0 com modelo imutável e sem retenção direta ou transitiva de `ISymbol`; manter as correções críticas já entregues no 0.1.14 cobertas por regressão.

**Critérios de aceite:** teste de cacheabilidade passa (edição em arquivo não relacionado não reexecuta o output); golden tests inalterados; **nenhum objeto alcançável** a partir dos valores cacheados retém `ISymbol`, `SyntaxNode`, `SyntaxTree`, `SemanticModel`, `Compilation`, `Diagnostic` ou `Location` (retenção direta ou indireta, ex.: `TypeDescriptor.Symbol` aninhado).

**Testes:** suítes completas + teste de steps incrementais novo.

### Resultado da Fase 14

**Concluída em 2026-07-12.**

- `RoyalCode.Extensions.SourceGenerator` 0.2.0 introduziu snapshots imutáveis de tipo, propriedade, caminho, assignment e match. A conversão ocorre no final do `Transform`; os snapshots preservam somente strings, flags, enums e coleções read-only, sem objetos Roslyn. O pacote foi publicado e consumido pelo SmartSelector.
- `AutoSelectInformation`, `AutoPropertiesInformation` e `AutoDetailsInformation` retêm apenas snapshots e `DiagnosticInfo`. Um modelo transitório (`AutoPropertiesBuildInformation`) mantém os descritores semânticos estritamente dentro do `Transform` e nunca cruza o limite incremental.
- `DiagnosticInfo` retém ID, argumentos, FilePath, TextSpan e LineSpan; `Diagnostic` é criado somente no source output. Como a API Roslyn não permite recriar uma `SourceFile Location` sem a `SyntaxTree`, a localização materializada é `ExternalFile`, preservando arquivo e span sem reter a árvore.
- O caminho pai de objetos `NewInstance` é incorporado recursivamente ao snapshot. `SelectLambdaGenerator` tornou-se uma emissão pura e não chama mais `MatchSelection.AddParentProperty`.
- `IncrementalPipelineTests` usa `trackIncrementalGeneratorSteps: true`: uma edição irrelevante mantém todos os source outputs em `Cached`/`Unchanged`; a travessia reflexiva do grafo, incluindo `AutoDetails`, rejeita `ISymbol`, `SyntaxNode`, `SyntaxTree`, `SemanticModel`, `Compilation`, `Diagnostic` e `Location`.
- Os goldens permaneceram inalterados. Gates com o pacote publicado: solution build — **0 erros, 8 warnings esperados**; suíte principal sem filtro — **80/80**; Demo — **28/28**; Benchmarks Release — **0 erros/0 warnings**; pacote externo — **36/36**.

---

## Fase 15 - Features incrementais de mapeamento

**Depende de:** Fases 11–14 (base estabilizada). Mantida neste plano por decisão do humano (achado 4 da análise externa rejeitado).

**Escopo:** atributos públicos, generators, docs, testes.

**O que/como:** features pequenas aprovadas nesta análise; cada uma com testes de geração + execução + EF quando aplicável.

**Tarefas:**

- [x] Adicionar `Exclude` (e `Flattening`) diretamente em `AutoSelectAttribute<TFrom>` sem exigir `AutoProperties`.
- [x] Suportar arrays (`T[]`) como propriedades simples em `IsSupportedType` e nas atribuições de coleção.
- [x] Suportar caminho aninhado em `MapFrom` (`[MapFrom("Address.City")]`) com diagnóstico para caminho inválido.
- [x] Documentar as três features em `docs.md` e README.

**Critérios de aceite:** cada feature tem teste de geração, teste de execução em memória e (quando envolver query) teste EF no Demo; docs atualizadas.

**Testes:** suítes completas verdes.

### Resultado da Fase 15

**Concluída em 2026-07-12.**

- `AutoSelectAttribute<TFrom>` passou a herdar `AutoPropertiesAttributeBase`. Quando `Exclude` ou `Flattening` é configurado diretamente, o próprio `AutoSelect` ativa a geração automática de propriedades; se `AutoProperties` também estiver presente, as configurações são combinadas.
- Arrays de tipos simples entram em `AutoProperties`. Um retriever semântico local trata `IArrayTypeSymbol` sem acionar a limitação de namespace do descritor externo. Arrays declarados de DTOs complexos projetam coleções/arrays de origem com `Select(...).ToArray()`; quando a origem nullable é nula, o fallback é `Array.Empty<T>()`.
- `MapFrom` aceita caminhos separados por ponto. O matcher valida cada segmento público e legível, restringe explicitamente a raiz para que um membro direto homônimo não vença o caminho configurado e preserva a política de null/EF no caminho real. Caminhos inválidos emitem **RCSS017** na propriedade do DTO.
- `MappingFeatureTests` cobre geração e execução real das três features, incluindo array complexo, array nullable e colisão entre `Address.City` e `AddressCity`. O Demo acrescenta duas projeções traduzidas pelo EF Core SQLite: configuração direta de `AutoSelect` e `MapFrom("Warehouse.Location")` por navegação nullable.
- README, docs, XML docs, analyzer releases e release notes foram atualizados. Nenhum arquivo de CI foi alterado, conforme decisão humana anterior.
- Gates finais: solution build — **0 erros, 8 warnings esperados**; suíte principal sem filtro — **84/84**; Demo — **30/30**; Benchmarks Release — **0 erros/0 warnings**.

---

## Matriz de rastreabilidade

| Objetivo | Fase(s) | Decisão(es) | Critério(s) de aceite | Teste(s) |
|---|---|---|---|---|
| 1. Zero bugs B1–B5 | 4, 10 | DF1, DF2, DF13 | cenários do harness compilam sem CS0108/CS0109 | testes novos das Fases 4 e 10 |
| 2. Sem falhas silenciosas | 5, 6 | DF3, DF7 | todo caso rejeitado emite RCSS com location | testes de diagnóstico da Fase 5 |
| 3. Código gerado auto-suficiente/limpo | 4 (B4), 11 | DF6, DF11 | compila sem ImplicitUsings; sem CS8618/CS1591; `[GeneratedCode]` gradado | compilação sem implicit usings; snapshots |
| 4. Compilação final validada + TDD zerados | 1, 5, 6, 13 | DF4, DF9, DF20 | zero `KnownLimitation` restante | `dotnet test` sem filtro |
| 5. Semântica de null definida | 12 | DF5, DF18 | `From` sem NRE nos casos da política | testes `NullPolicy*` + Demo |
| 6. Pipeline cache-friendly | 0, 14 | — | steps `Cached` após edição irrelevante | teste de steps incrementais |
| 7. CI com gates | 9 | DF9, DF19 | release só com CI verde e procedência por `ci_run_id`; baseline nominal de limitações | execução de workflows |
| 8. Documentação fiel | 2 | DF10 | README sem exemplo que gera RCSS003 | teste de compilação de snippets |
| 9. Features incrementais de mapeamento | 15 | — | opções diretas, arrays e caminhos MapFrom compilam/executam e traduzem quando aplicável | `MappingFeatureTests` + `MappingFeatureDemoTests` |

---

## Invariantes a preservar

1. Expressões geradas permanecem traduzíveis por EF Core — suíte do Demo (SQLite) verde em toda fase.
2. Nomes dos membros gerados (`Select{TFrom}Expression`, `From`, `Select{Dto}`, `To{Dto}`) não mudam sem decisão fechada.
3. Generator permanece netstandard2.0 e livre de dependências fora de `Microsoft.CodeAnalysis.*` + `RoyalCode.Extensions.SourceGenerator`.
4. Golden tests nunca são atualizados no mesmo commit que altera o emissor sem diff revisado por humano ou justificativa no `Resultado da Fase`.
5. Testes TDD de limitação nunca recebem `Skip`; nenhuma nova limitação entra sem decisão registrada neste plano (DF9).
6. API pública dos atributos só muda dentro do aprovado por DF16/DF17 e pela Fase 15 (`AutoSelect` na classe base); qualquer outra mudança exige nova decisão.

---

## Critérios globais de conclusão

- `dotnet build SmartSelector.sln` sem warnings CS8618/CS0108/CS0109 originados de código do repositório ou gerado.
- `dotnet test RoyalCode.SmartSelector.Tests\RoyalCode.SmartSelector.Tests.csproj` verde sem filtro (teste de DTO genérico convertido em teste de diagnóstico, DF20).
- `dotnet test RoyalCode.SmartSelector.Demo\RoyalCode.SmartSelector.Demo.csproj` verde.
- Harness de casos-limite (B1–B5, órfão, fully-qualified, homônimos) sem falha silenciosa nem erro CS inesperado.
- Baseline nominal de `KnownLimitation` zerada; alterações adicionais no mecanismo de CI/release foram explicitamente diferidas por decisão humana na Fase 9.
- `Perguntas ao humano` vazia ou explicitamente diferida.

---

## Riscos

| Risco | Gatilho | Impacto | Mitigação | Estado |
|---|---|---|---|---|
| Piso Roslyn 5.6 exclui consumidores net8/net9 | publicação do pacote antes da Fase 8 | pacote inutilizável para parte do público | DF15 multi-target validado pela matriz 8/9/10 da Fase 0 antes de qualquer release | Aberto |
| Seleção multi-target Roslyn (DF15) regride no empacotamento real | alteração futura no `.nupkg` ou SDK | variante incompatível carregada pelo consumidor | spike fechou SDK 8→4.8, SDK 9→4.8 e SDK 10→5.6 com build+run; repetir a matriz no CI | Mitigado |
| Mudanças no pacote externo bloqueiam Fases 8, 11, 13 e 14 | `TypeDescriptor`/`ClassGenerator` sem API necessária | fases aguardando release NuGet do pacote (fonte local disponível) | releases pequenos e antecipados: 0.1.14 (pin 4.8 + caches/equality), 0.1.15 (nullable + containing types), 0.2.0 (modelo sem símbolos) | Aberto |
| Harness aprova código incompatível com TFMs antigos | testes compilando apenas contra TPA (net10) | incompatibilidade net8/net9 despercebida | reference assemblies por TFM na Fase 1 + matriz de SDKs da Fase 0 | Aberto |
| Atualização em massa de snapshots mascara regressão | Fases 4, 11, 12 alteram todos os golden tests | código gerado incorreto aprovado | commit separado para snapshots + validação de compilação da Fase 1 | Aberto |
| Condicionais de null quebram tradução EF em provedores específicos | Fase 12 | queries falham em runtime do consumidor | validar via Demo SQLite; documentar provedores testados | Aberto |
| Gate de CI bloqueado pelos TDD vermelhos | Fase 9 ativada antes da categorização | CI vermelho permanente | Fase 1 (traits) obrigatória antes da Fase 9 | Aberto |
| Dívida `KnownLimitation` cresce sem controle | novas limitações adicionadas fora do plano | testes vermelhos permanentes esquecidos | governança DF9: vínculo com fase, decisão registrada, contagem no CI | Aberto |

---

## Diferidos e backlog

- Suporte a records e membros `init`/`required` — destino: backlog.
- Interfaces como tipo de origem (`TypeKind.Class` exigido hoje) — destino: backlog.
- CodeFix providers (adicionar `partial`, criar propriedade correspondente) — destino: backlog.
- Renomear `{Dto}_Extensions` → `{Dto}Extensions` (CA1707) — destino: backlog; explicitamente fora deste plano (breaking sem benefício funcional imediato).
- Transformar snippets do README em testes compiláveis além do mínimo da Fase 2 — destino: fase futura de docs.

---

## Referências

- `C:\git\RoyalCode\Utils\RoyalCode.Utils\RoyalCode.Extensions.SourceGenerator` — código-fonte local do pacote externo (testes em `...\RoyalCode.Extensions.SourceGenerator.Tests`; solution `C:\git\RoyalCode\Utils\RoyalCode.Utils\Util.sln`). Modificações exigem release NuGet.
- `.ai/reviews/spike-viabilidade-externa-2026-07-11.md` — relatório da Fase 0 (matriz DF15, capacidades do pacote externo, divisão recomendada entre 0.1.14/0.1.15/0.2.0).
- `scratchpad/spike/run-spike.ps1` — reprodução da matriz SDK 8/9/10; `last-run.md` registra o último resultado e o hash do pacote.
- `.ai/reviews/smart-selector-review-2026-07-10.md` — revisão técnica anterior (itens 1–8).
- `.ai/templates/template-ai-implementation-plan.md` — template deste plano.
- Análise externa do plano (2026-07-11) — 10 achados; avaliação registrada no Histórico de decisões.
- `RoyalCode.SmartSelector.Tests/Tests/GeneratedCodeCompilationTddTests.cs` — testes TDD de limitações conhecidas.
- `RoyalCode.SmartSelector.Generators/AnalyzerDiagnostics.cs` — IDs RCSS000–005 atuais.
- `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md` — rastreio de diagnósticos.
- Análise de código de 2026-07-11 (conversa Claude Code) — bugs B1–B5 e harness de casos-limite.
