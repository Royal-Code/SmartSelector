# Revisão das Fases 1–9 — Plano `smartselector-hardening`

**Data:** 2026-07-12
**Plano revisado:** `.ai/plans/plan-smartselector-hardening.md`
**Método:** inspeção direta do código, testes, csproj, pacotes e workflows no working tree (branch `main`, limpo, HEAD `10767ac`), mais reexecução dos gates declarados pelo plano. Cada tarefa marcada `[x]` foi conferida contra o artefato correspondente; os números de teste reportados nos "Resultados de Fase" foram reproduzidos localmente.

## Sumário executivo

| Fase | Estado no plano | Veredito desta revisão |
|---|---|---|
| 1 - Harness de testes | Concluída | **Completa** — todos os critérios de aceite verificados |
| 2 - Typos e documentação | Concluída | **Completa** |
| 3 - Limpeza Demo/Benchmarks | Concluída | **Completa** |
| 4 - Bugs de baixo risco (B1/B3/B4/B5) | Concluída | **Completa** — 5 testes de hardening cobrem os 4 bugs |
| 5 - Diagnósticos completos | Concluída | **Completa** — RCSS006–011 com location e testes |
| 6 - AutoProperties<T> semântico | Concluída | **Completa** — caminho sintático removido; baseline caiu para 2 |
| 7 - Refactors internos | Concluída | **Completa** — DF16/DF17 aplicadas; API pública snapshotada |
| 8 - Empacotamento e dependências | Tabela: "Pendente"; corpo: "concluída" | **Completa no repositório** — porém plano internamente inconsistente (ver achados) |
| 9 - CI e release com gates | Tabela: "Pendente"; corpo: "implementação local concluída" | **Parcial por design** — workflows prontos e commitados; ativação no GitHub pendente (última tarefa `[ ]`) |

**Gates reexecutados em 2026-07-12:**

- `dotnet build SmartSelector.sln` — **0 erros, 16 warnings**, todos em `Tests/Models/Expected` (CS8618/CS0108/CS8601/CS8604), mapeados para as Fases 11/12.
- `dotnet test ... --filter "Category!=KnownLimitation"` — **54/54 aprovados**.
- `dotnet test ... --filter "Category=KnownLimitation"` — **exatamente 2/2 falhando por design** (genérico e aninhado; ambos apontando a Fase 13).
- `dotnet test RoyalCode.SmartSelector.Demo` — **25/25 aprovados** (EF Core + SQLite).

Os números batem exatamente com os registrados nos Resultados das Fases 7/8/9 do plano. Nenhuma divergência entre o que o plano afirma ter sido entregue e o que existe no código foi encontrada, com exceção das inconsistências de bookkeeping do próprio plano (ver Fase 8 e achados transversais).

---

## Fase 1 — Harness de testes com validação de compilação

**Completude: total.** Todas as 6 tarefas verificadas em `RoyalCode.SmartSelector.Tests/Util.cs`:

- `CompileResult` é um record com os quatro membros prometidos: `GeneratorDiagnostics`, `CompilationDiagnostics`, `GeneratedSources` (dicionário por hintName) e `RunResult` (`GeneratorDriverRunResult`), mais helpers `Errors`/`Warnings`/`GeneratedSource(hintName)`.
- `CompileAndAssert` itera `Net80`, `Net90` e `Net100` usando `Basic.Reference.Assemblies` isoladas por `extern alias` (`net80::`, `net90::`, `net100::`) e falha por padrão em qualquer erro CS; `assertNoWarnings` é opt-in, como especificado.
- `CompileFast` mantém o TPA como caminho rápido deliberado, com XML doc explicando quando usá-lo — exatamente a gradação prevista em DF4.
- As fontes dos 4 atributos runtime são recursos embutidos (`RuntimeSources.*`) compilados em cada TFM, evitando referenciar a DLL do test host net10 ao validar net8/net9 — resolve a lacuna do TPA apontada no contexto do plano.
- `grep` por `SyntaxTrees.Skip` em toda a suíte: **0 ocorrências**.
- Os testes TDD em `GeneratedCodeCompilationTddTests.cs` usam `[Trait("Category", "KnownLimitation")]` sem `Skip`, cada um com comentário apontando a Fase 13 (o terceiro, fully-qualified, já saiu do trait na Fase 6, como previsto).

**Achados:** nenhum problema. Detalhe menor, sem impacto: `CompileAndAssert` retorna o `CompileResult` da última iteração (net10), então snapshots comparados após o loop refletem apenas o net10 — aceitável porque a geração é idêntica entre TFMs e os erros já foram validados por TFM.

## Fase 2 — Typos e documentação

**Completude: total.** Verificado por busca textual e pelos testes criados:

- Nenhuma ocorrência de `AutoSelect<Order>` combinado com `AutoProperties<Order>` em `README.md`/`docs.md`; a seta `→` do exemplo de flattening está correta; os exemplos usam `{ get; } =` (1 ocorrência no README, 2 no docs) e nenhum `SelectXxxExpression =>` restante.
- A supressão obsoleta de `MapFromAttribute.cs` não existe mais (nenhum `SuppressMessage` no arquivo); `InternalVisible.cs` começa direto no `using`, sem linha em branco inicial.
- `DocumentationSnippetCompilationTests` tem os 4 cenários prometidos (quickstart, `AutoProperties<TFrom>` isolado, aninhado com `AutoDetails`, flattening), todos via `CompileAndAssert` (logo validados em net8/net9/net10).
- `AnalyzerDiagnosticMessageTests` protege por regressão o nome `AutoPropertiesAttribute` na mensagem RCSS003, cuja string atual em `AnalyzerDiagnostics.cs` está correta.

**Achados:** nenhum.

## Fase 3 — Limpeza de Demo e Benchmarks

**Completude: total.**

- Os 6 membros do cenário Library usam `required` (`Book.Title/Author/ISBN/Shelf`, `Shelf.Location`, `BookDetails.Shelf`); nenhum `#nullable disable` introduzido (DF14 respeitada). O build atual não emite nenhum CS8618 vindo do Demo — os 16 warnings restantes estão todos em `Tests/Models/Expected`.
- `Benchmarks/Program.cs` contém apenas o `BenchmarkSwitcher`; sem `Hello, World!`. Nenhuma ocorrência de `Generated_CachedDelegate` ou da variável `now` no projeto de benchmarks.
- Demo reexecutado: 25/25 verdes.

**Achados:** nenhum.

## Fase 4 — Bugs de geração de baixo risco (B1, B3, B4, B5)

**Completude: total.** A implementação central está em `GeneratedSourceConventions.cs`:

- **B1 (hintName):** `FileName(type, generatedTypeName, category)` monta `{namespace}.{containing types}.{nome}.{categoria}.g.cs`, com a cadeia de containing types via `ContainingType` e aridade embutida via `MetadataName` — formato compatível com DF1. Teste `Homonymous_dtos_in_different_namespaces_should_have_unique_hint_names` cobre a colisão original.
- **B3 (get-only):** teste `AutoProperties_should_not_duplicate_a_declared_get_only_property` verde.
- **B4 (usings):** `ApplyRequiredNamespaces` injeta `using System;`, `using System.Linq;` e `using System.Collections.Generic;` via evento `Generating`; o harness ganhou `includeImplicitUsings: false` e o teste `Generated_files_should_compile_without_implicit_usings` compila nesse modo.
- **B5 (`new`):** par de testes `Generated_members_should_hide_accessible_base_members_with_new` / `Generated_members_without_base_conflicts_should_not_use_new` cobre o positivo e o falso positivo, conforme DF13.

Os 5 testes prometidos existem em `GenerationHardeningTests.cs` (5 `[Fact]`).

**Achados:** nenhum problema funcional. Observação de forma: a aridade no hintName vem do `MetadataName` (formato `` Nome`1 ``), não de um sufixo dedicado como o exemplo de DF1 sugere; irrelevante na prática porque DTOs genéricos são rejeitados por RCSS008 (DF20), mas vale conferir o formato quando a Fase 13 exercitar containing types.

## Fase 5 — Diagnósticos completos e localizados

**Completude: total.**

- `AnalyzerDiagnostics.cs` define RCSS006 (não-partial), RCSS007 (`AutoProperties` órfão), RCSS008 (DTO genérico), RCSS009 (DTO aninhado), RCSS010 (flattening ambíguo, **Warning**) e RCSS011 (namespace global — o achado extra do spike da Fase 0, devidamente incorporado). Severidades conforme o plano.
- Os 6 IDs estão registrados em `AnalyzerReleases.Unshipped.md` — e apenas eles: RCSS012, criado na Fase 5 e removido na Fase 6 antes de publicar, não deixou rastro nem no arquivo nem no código, exatamente como o Resultado da Fase 6 declara.
- Nenhuma ocorrência restante de `Location.None` ou `location: null` nos generators (a regressão do RCSS001 de `AutoDetailsGenerator` foi corrigida).
- `GeneratorDiagnosticTests` tem 7 `[Fact]` — um por ID novo (6) mais a regressão de location do RCSS001, cumprindo o critério "asserção de ID + location".
- O par exigido por DF9 está de pé: RCSS008/RCSS009 têm testes verdes de diagnóstico enquanto os dois testes de compilação correspondentes continuam vermelhos sob `KnownLimitation`.

**Achados:** nenhum.

## Fase 6 — AutoProperties<T> semântico

**Completude: total.**

- `AutoPropertiesGenerator.Transform` resolve o atributo por `context.Attributes` e lê `Exclude`/`Flattening` de `NamedArguments`; a sintaxe (`ApplicationSyntaxReference`) é usada apenas para location de diagnóstico. Nenhuma ocorrência de `IdentifierNameSyntax`/`GenericNameSyntax` restou no arquivo.
- O teste TDD `Generated_code_should_compile_with_a_fully_qualified_AutoProperties_attribute` está **sem trait** e no gate padrão (verde — incluído nos 54/54).
- A baseline `KnownLimitation` caiu de 3 para **exatamente 2**, confirmado por execução: os dois testes restantes (genérico e aninhado) falham como esperado.
- `AutoPropertiesSemanticTests` cobre a equivalência entre forma simples, `global::` e alias, incluindo `MapFrom` por alias (proteção da remoção do fallback sintático de `MapFromPropertyNameResolver`).

**Achados:** nenhum. A decisão de remover RCSS012 antes de publicá-lo foi correta e está consistente em código, testes e `AnalyzerReleases.Unshipped.md`.

## Fase 7 — Refactors internos do generator

**Completude: total.**

- `GeneratedSourceConventions.ApplyDeclaredAccessibility` centraliza o mapeamento de acessibilidade, com fallback público e tratamento explícito de `ProtectedOrInternal`/`ProtectedAndInternal`; `InformationEquality` (com `InformationEqualityTests` dedicado) e `GeneratorSyntaxPredicates` eliminam as duplicações citadas.
- DF16 aplicada: os 5 atributos públicos (`AutoSelectAttribute<TFrom>`, `AutoPropertiesAttribute`, `AutoPropertiesAttribute<TFrom>`, `AutoDetailsAttribute`, `MapFromAttribute`) são `sealed`.
- DF17 aplicada: `AutoPropertiesAttributeBase` (pública, abstrata) declara `Exclude`/`Flattening` e é base das duas formas de `AutoProperties` e de `AutoDetails`.
- `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` existem no projeto runtime; o Unshipped registra corretamente as entradas `*REMOVED*` dos membros cujo declaring type mudou e as adições da base — o formato correto para mover membros de tipo (não é remoção de API, é realocação, e o snapshot documenta isso).
- `PublicAttributeContractTests` comprova os CS0509 para consumidores que tentem herdar.
- `Directory.Build.props`: `Ver` = **0.5.0**, com comentário registrando a decisão de `AssemblyVersion` na série 0.x. `RELEASE_NOTES.md` documenta breaking changes, recompilação, reflexão com `DeclaredOnly` e migração, e está empacotado (`None Include="../RELEASE_NOTES.md" Pack="true"`).
- Golden tests intactos (54/54 sem alteração de snapshot registrada), cumprindo o critério "refactor sem mudança de texto gerado".

**Achados:** nenhum.

## Fase 8 — Empacotamento e dependências

**Completude: total no que é verificável neste repositório.** As 6 tarefas estão `[x]` e conferem:

- `RoyalCode.SmartSelector.Generators.csproj` usa `GeneratePathProperty="true"` + `$(PkgRoyalCode_Extensions_SourceGenerator)`; `NoWarn` contém apenas `NU5128` (o `NU1900` foi removido e o restore atual não o reintroduz).
- DF15 implementada pelo target `BuildRoslynVariants`: rebuild com `RoslynVersion=4.8.0` e `5.6.0` em `obj/bin` separados, empacotando ambos.
- Inspeção real do `RoyalCode.SmartSelector.Generators.0.5.0.nupkg`: contém `RoyalCode.SmartSelector.Generators.dll` **e** `RoyalCode.Extensions.SourceGenerator.dll` em `analyzers/dotnet/roslyn4.8/cs` e `analyzers/dotnet/roslyn5.6/cs`, mais o `.props` em `build/` — layout exato do critério de aceite.
- `libs.targets` fixa `ExtSrcGenVer` = **0.1.14**; o pacote 0.1.14 está presente no cache NuGet e o restore/build da solução funciona com ele.
- README documenta a matriz SDK→variante (8.0.422→roslyn4.8; 9.0.100→roslyn4.8; 10.0.301→roslyn5.6) com os requisitos mínimos por variante.

**Achados:**

1. **Inconsistência de bookkeeping no plano (o achado que motivou esta revisão):** o corpo da Fase 8 declara "Estado: concluída" com todas as tarefas `[x]`, mas a tabela de progresso ainda diz "Pendente", a barra marca 50% (8 de 16) e o Status do cabeçalho diz "Fases 0–7 concluídas; próxima: Fase 8". Pela regra de manutenção do próprio plano, a tabela e a barra deveriam ter sido atualizadas (9 de 16, ~56%). **Recomendação:** marcar a Fase 8 como `Concluida em 2026-07-12` na tabela e atualizar barra e cabeçalho.
2. Itens externos ao repositório (publicação do 0.1.14 no NuGet.org, commit `b141485` no repo `RoyalCode/Utils`, matriz de consumo com SDKs reais) não são verificáveis daqui; o Resultado da Fase descreve a validação feita (restore limpo sem feed local, binário idêntico nas duas pastas) e as evidências locais são consistentes com isso — o pacote 0.1.14 restaurado do feed público está no cache e o build funciona. Sem sinal de divergência.

## Fase 9 — CI e release com gates

**Completude: parcial, e o plano reconhece isso corretamente.** 5 de 6 tarefas `[x]`; a última (`configurar environment nuget-production, checks obrigatórios e executar aceite no GitHub`) permanece `[ ]`, e o Resultado da Fase afirma explicitamente que a fase não deve ser marcada concluída antes desses gates serem observados no GitHub. Estado coerente.

O que foi verificado nos workflows commitados (`10767ac`, únicos arquivos em `.github/workflows/` — o legado `smart-select.yml` foi removido, eliminando as referências a SmartSearch):

- **`ci.yml`:** dispara em PR, push para `main`, tags `v*` e manual; job `build-test-pack` faz restore/build Release, gate `Category!=KnownLimitation`, testes do Demo, pack dos dois projetos com validação de "exatamente 1 pacote de cada" e de versões idênticas, manifesto JSON com `source_sha`/`source_ref`/SHA-256 por pacote, e smoke test de consumo sob SDK 8 com fonte NuGet contendo **apenas** os pacotes produzidos (`<clear/>` + feed local). Artefato único `nuget-packages` com os dois `.nupkg` + manifesto.
- **Job `known-limitations-baseline`:** valida a baseline **nominal** de DF9 — os dois nomes de teste hardcoded, falhando o job se houver teste inesperado, ausente, aprovado ou com outcome diferente de `Failed`, e publica tabela no summary. Implementa fielmente a semântica "falha esperada não bloqueia; desvio da baseline bloqueia".
- **`release.yml`:** `workflow_dispatch` com `ci_run_id` obrigatório; valida que o run pertence a `.github/workflows/ci.yml`, concluiu com `success`, e que o ref é `main` (com SHA igual ao HEAD atual de `main`) ou tag semântica com SHA correspondente; baixa o artefato do run **sem checkout, build ou pack**; valida manifesto (schema, SHA, ref, exatamente 2 pacotes + manifesto, nomes por regex, hashes SHA-256 recalculados, ausência de pacote não declarado); o job de publicação usa o environment `nuget-production` (ponto de aprovação DF19), reverifica os hashes após a aprovação e publica cada arquivo por caminho explícito. Todos os requisitos do achado 7 da segunda análise externa estão implementados.

**Achados:**

1. **Pendência real e corretamente sinalizada:** nada do lado GitHub foi ativado ainda — environment `nuget-production` com reviewer, checks obrigatórios em `main` (`Build, test, pack and consume` e `Known limitations baseline`), PR de teste e os casos negativos/positivo do release. Até lá, os critérios de aceite "PR com teste falhando bloqueia merge" e "release recusa `ci_run_id` inválido" estão implementados mas **não observados**. É o próximo passo natural do plano.
2. Observação (não bloqueia): `dotnet nuget push` usa `--skip-duplicate`, então uma re-execução do release com a mesma versão termina verde sem publicar nada — comportamento razoável para idempotência, mas vale saber que "verde" não significa necessariamente "publicou".
3. Observação (design consciente): a exigência de que o SHA do run seja o HEAD **atual** de `main` significa que qualquer commit posterior invalida o run de CI para release — é a semântica mais segura, apenas exige rodar release logo após o CI do commit desejado, ou usar tag.
4. Observação: a baseline nominal duplica os nomes dos testes entre `ci.yml` e a suíte; é inerente ao desenho de DF9 (baseline por nome no CI) e será atualizada nas Fases 13 (para 0). O risco de esquecer a atualização é coberto pelo próprio job, que falha quando um teste da baseline passa.

---

## Achados transversais e recomendações

1. **Atualizar o bookkeeping do plano:** tabela de progresso, barra e linha de Status estão defasados em relação ao corpo (Fase 8 concluída). Sugestão de estado correto: 9 de 16 fases seria prematuro — a Fase 9 de fato **não** está concluída; o correto hoje é **9 de 16 concluídas contando a Fase 0** (Fases 0–8), barra `█████████░░░░░░░`, "56%", Status "Fases 0–8 concluídas; Fase 9 implementada localmente, aguardando ativação no GitHub".
2. **Fase 9 — próximos passos concretos:** push dos workflows já está feito; falta (a) criar o environment `nuget-production` com required reviewer, (b) marcar os dois jobs de CI como required checks de `main`, (c) abrir PR de teste com falha proposital para observar o bloqueio, (d) exercitar o release com `ci_run_id` inválido (run falho, branch errada) e um válido. Só então marcar a fase.
3. **Qualidade geral:** o padrão de execução das Fases 1–8 é alto e disciplinado — cada fase tem testes novos nomeados, os gates reportados são reproduzíveis (54/54, 2/2, 25/25 e 0 erros/16 warnings confirmados hoje), as decisões DF são rastreáveis no código (DF1, DF3, DF4, DF9, DF13–DF17 todas verificadas em artefatos concretos) e não encontrei nenhuma tarefa marcada `[x]` sem implementação correspondente.
4. **Warnings remanescentes (16):** todos em `Tests/Models/Expected` (CS8618 do campo de cache non-nullable, CS0108 em `BlogsPosts.cs`, CS8601/CS8604 em `Nulls.cs`) — são exatamente os alvos das Fases 11 (nullable-clean) e 12 (política de null), como o plano prevê. Nenhuma ação agora.
5. **B2 continua aberto** (AutoDetails com nome fora da convenção) — correto, pertence à Fase 10, próxima fase de comportamento após fechar a Fase 9.
