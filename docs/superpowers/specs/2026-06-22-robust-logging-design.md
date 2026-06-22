# Design: Logging Robusto — AjuriIA API

**Data:** 2026-06-22
**Status:** Aprovado

---

## Objetivo

Adicionar logging estruturado e completo ao AjuriIA API usando Serilog, com visibilidade de LLM calls, startup, chat e erros. Output legível em desenvolvimento, JSON compacto em produção para compatibilidade com aggregators futuros.

---

## Abordagem: Middleware de Enriquecimento + Eventos Estruturados

Um `RequestEnrichmentMiddleware` empurra propriedades para o `LogContext` do Serilog no início de cada requisição. Todo log emitido depois (services, orchestrator, controller) herda essas propriedades automaticamente, sem passagem manual de parâmetros.

O middleware **não intercepta response** — só enriquece contexto e chama `next`. Responsabilidade única e clara.

---

## Arquitetura

### Novos arquivos
- `src/AjuriIA.API/Middleware/RequestEnrichmentMiddleware.cs` — empurra `RequestId`, `ProfileId`, `ClientIp` no `LogContext`

### Arquivos modificados
- `src/AjuriIA.API/Program.cs` — registrar middleware, configurar JSON formatter em produção, adicionar logging de startup
- `src/AjuriIA.API/Services/LLMOrchestratorService.cs` — adicionar logs de tentativa, primeiro chunk, fallback e falha total
- `src/AjuriIA.API/Controllers/ChatController.cs` — adicionar log final de `[CHAT]` com profile, llm, chunks, duration
- `src/AjuriIA.API/appsettings.json` — ajustar níveis por ambiente

### Arquivos de teste
- `tests/AjuriIA.Tests/Unit/Middleware/RequestEnrichmentMiddlewareTests.cs` — novo
- `tests/AjuriIA.Tests/Unit/Services/LLMOrchestratorServiceTests.cs` — adicionar casos de log

---

## Eventos de Log

### Startup
```
[INF] AjuriIA iniciando — Environment: Production
[INF] CLAUDE_API_KEY: ✓  |  OPENAI_API_KEY: ✗ não configurada  |  GEMINI_API_KEY: ✓
```
- Chave ausente → `LogWarning` (não `LogError` — é configuração, não falha em runtime)
- Chave presente → `LogInformation`

### RequestEnrichmentMiddleware
Sem log próprio. Empurra no `LogContext` via `LogContext.PushProperty`:
- `RequestId` — `HttpContext.TraceIdentifier`
- `ProfileId` — extraído do body JSON (`profileId`) se presente; caso contrário `"unknown"`. Requer `Request.EnableBuffering()` antes da leitura para não consumir o stream do controller.
- `ClientIp` — `HttpContext.Connection.RemoteIpAddress`

### LLMOrchestratorService
```
[INF] [LLM] Tentando {LlmName} para perfil {ProfileId}
[INF] [LLM] Primeiro chunk em {ElapsedMs}ms — {LlmName} respondeu
[WRN] [LLM] {LlmName} falhou no primeiro chunk — {ExceptionType}: {HttpStatus} {ErrorBody}
[INF] [LLM] Fallback para {LlmName}
[ERR] [LLM] Todos os LLMs falharam para {ProfileId} — tentados: {LlmNames}
```
- `{ErrorBody}` capturado via `HttpRequestException.Message` ou leitura do response body quando possível
- Propriedades nomeadas (não interpolação) para structured logging

### ChatController
```
[INF] [CHAT] profile={ProfileId} llm={LlmUsed} chunks={ChunkCount} duration={ElapsedMs}ms
```
- Emitido ao fechar o stream (no `finally` do response)
- `ChunkCount` = total de chunks SSE enviados
- `ElapsedMs` = `Stopwatch` iniciado antes do `await foreach`

### ExceptionHandlerMiddleware (sem mudança de código)
- `ProfileId` aparece automaticamente via enrichment do middleware
- Stack trace completo em `LogError` já existente

---

## Output por Ambiente

### Development (template legível)
```
[HH:mm:ss LVL] mensagem
```
Mantém o template atual. Nível mínimo: `Debug`.

### Production (JSON compacto — Render)
```json
{"@t":"2026-06-22T09:39:12Z","@l":"Information","@m":"[CHAT] profile=professor llm=gemini-flash chunks=47 duration=4320ms","ProfileId":"professor","RequestId":"0HN...","ClientIp":"::1"}
```
`WriteTo.Console(new CompactJsonFormatter())` — uma linha por evento, sem formatação extra.
Nível mínimo: `Information`. Microsoft/System em `Warning`.

Distinção feita por `ctx.HostingEnvironment.IsProduction()` no `Program.cs`.

---

## Testes

### `RequestEnrichmentMiddlewareTests.cs` (novo)
- Dado body com `profileId`, quando o middleware executa, então `LogContext` contém `ProfileId` = valor do body
- Dado body sem `profileId`, então `ProfileId` = `"unknown"`

### `LLMOrchestratorServiceTests.cs` (adições)
- Dado LLM primário falha, quando streaming, então `LogWarning` é chamado com `[LLM]` e nome do LLM
- Dado todos os LLMs falham, então `LogError` é chamado com lista de nomes tentados
- Dado LLM primário responde, então `LogInformation` é chamado com primeiro chunk elapsed

### `ChatControllerTests.cs` (adições)
- Dado chat bem-sucedido, quando stream fecha, então `LogInformation` é chamado com `[CHAT]` contendo profile, llm, chunks, duration

Todos os testes de logger usam `NSubstitute` para `ILogger<T>` — padrão existente no projeto.

---

## Critérios de Sucesso

1. Nos logs do Render, uma requisição `/api/chat` mostra: qual LLM foi tentado, tempo até primeiro chunk, total de chunks e duração total
2. Quando um LLM falha, o log mostra o status HTTP e o corpo do erro da API (ex: `429 rate_limit_exceeded`)
3. No startup, fica visível quais chaves estão e quais não estão configuradas
4. Em desenvolvimento, o output continua legível no terminal
5. Em produção, cada evento é uma linha JSON — pronto para Datadog, Loki ou qualquer aggregator
6. Os 41 testes existentes continuam passando

---

## O Que Não Entra

- Log do conteúdo das mensagens (privacidade)
- Log do system prompt (dados internos)
- Métricas (Prometheus, OpenTelemetry) — logging é suficiente para o momento
- Sinks adicionais (arquivo, banco) — Render captura o stdout automaticamente
