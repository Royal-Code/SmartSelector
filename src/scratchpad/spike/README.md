# Spike de compatibilidade Roslyn

Experimento reproduzível da Fase 0 do plano de hardening. Ele:

1. compila `RoyalCode.Extensions.SourceGenerator` e o SmartSelector generator contra Roslyn 4.8 e 5.6;
2. empacota as duas variantes em `analyzers/dotnet/roslyn{versão}/cs`;
3. restaura, compila e executa um consumidor `net8.0` com os SDKs 8, 9 e 10;
4. confirma, pelos logs diagnósticos, qual pasta de analyzer cada SDK carregou.

Execute no PowerShell:

```powershell
.\run-spike.ps1
```

Os binários, caches, binlogs e logs ficam em `artifacts/` e são ignorados. O resultado resumido e versionável é gravado em `last-run.md`.

