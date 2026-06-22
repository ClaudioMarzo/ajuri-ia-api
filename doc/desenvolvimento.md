# AjuriIA API — Guia de Desenvolvimento

## Setup inicial

### 1. Pré-requisitos
- .NET 10 SDK
- `make` (já vem no Linux/WSL2)

### 2. Clonar e configurar
```bash
git clone https://github.com/SEU_USUARIO/ajuri-ia-api
cd ajuri-ia-api
make setup
```

O `make setup` cria `src/AjuriIA.API/appsettings.Development.json`. Edite o arquivo e coloque suas chaves de API:

```json
{
  "CLAUDE_API_KEY": "sk-ant-...",
  "OPENAI_API_KEY": "sk-...",
  "GEMINI_API_KEY": "AIza...",
  "AllowedOrigin": "http://localhost:5173"
}
```

> Onde obter as chaves: veja `docs/setup/deploy-and-llms.md`

---

## Comandos do dia a dia

```bash
make run              # Sobe a API em http://localhost:5000
make watch            # Sobe com hot reload (reinicia ao salvar)
make test             # Roda os 41 testes
make test-unit        # Só testes unitários (mais rápido)
make test-integration # Só testes de integração
make build            # Compila sem subir
make clean            # Limpa bin/ e obj/
```

---

## Estrutura do projeto

```
src/AjuriIA.API/
├── Controllers/        # Recebe HTTP, delega para services
│   ├── ChatController.cs       → POST /api/chat (SSE)
│   └── ProfilesController.cs   → GET /api/profiles, /api/health
├── Services/           # Lógica de negócio
│   ├── ILLMService.cs          → Interface dos 3 serviços de LLM
│   ├── LLMOrchestratorService.cs → Fallback entre LLMs
│   ├── ProfileService.cs       → Carrega profiles.json
│   ├── ClaudeService.cs        → Anthropic API
│   ├── OpenAIService.cs        → OpenAI API
│   └── GeminiService.cs        → Google Gemini API
├── Models/             # DTOs (sem lógica)
├── Validators/         # FluentValidation
├── Middleware/         # ExceptionHandlerMiddleware
└── Config/
    └── profiles.json   # 6 perfis com system prompts

tests/AjuriIA.Tests/
├── Unit/               # Testes sem HTTP (puro C#)
├── Integration/        # Testes de endpoint com WebApplicationFactory
└── Functional/         # Fluxos completos end-to-end
```

---

## Padrão de testes (BDD)

Cada teste segue três regras:
1. `[Fact(DisplayName = "Given X, When Y, Then Z")]`
2. Blocos internos `// Given`, `// When`, `// Then`
3. **Uma única asserção** `Should()` por teste

```csharp
[Fact(DisplayName = "Given primary LLM fails, When streaming, Then uses fallback LLM")]
public async Task Given_PrimaryLLMFails_When_Streaming_Should_UseFallbackLLM()
{
    // Given
    var primary = Substitute.For<ILLMService>();
    primary.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
           .Throws<HttpRequestException>();
    var fallback = Substitute.For<ILLMService>();
    fallback.Name.Returns("gpt-4o-mini");
    fallback.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Helpers.MockChunks("resposta"));

    var sut = new LLMOrchestratorService([primary, fallback]);

    // When
    await foreach (var _ in sut.StreamAsync(perfil, "mensagem", default)) { }

    // Then
    sut.LastUsedLlm.Should().Be("gpt-4o-mini");
}
```

---

## Logs

A API usa Serilog com o formato:
```
[HH:mm:ss LVL] Mensagem
[HH:mm:ss LVL] HTTP GET /api/profiles → 200 (12ms)
```

Níveis: `DBG` (debug), `INF` (info), `WRN` (warning), `ERR` (error), `FTL` (fatal)

---

## Variáveis de ambiente

| Variável          | Descrição                              | Default local      |
|-------------------|----------------------------------------|--------------------|
| `CLAUDE_API_KEY`  | Chave Anthropic                        | —                  |
| `OPENAI_API_KEY`  | Chave OpenAI                           | —                  |
| `GEMINI_API_KEY`  | Chave Google Gemini                    | —                  |
| `AllowedOrigin`   | Domínio do frontend (CORS)             | `localhost:5173`   |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production`  | `Development`      |
