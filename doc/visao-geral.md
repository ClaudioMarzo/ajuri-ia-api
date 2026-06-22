# AjuriIA API — Visão Geral

## O que é

Backend da plataforma AjuriIA — assistência por inteligência artificial construída para a realidade amazônica. O nome vem do Nheengatu e significa *mutirão*: trabalhar junto.

O sistema recebe uma mensagem do usuário junto com um perfil (professor, produtor de guaraná, pescador etc.) e retorna uma resposta em streaming gerada por IA, calibrada para o contexto amazônico.

---

## Arquitetura

```
HTTP POST /api/chat
        │
        ▼
 ChatController
        │  valida com ChatRequestValidator (FluentValidation)
        │  carrega perfil + system prompt
        ▼
 LLMOrchestratorService
        │  escolhe o LLM primário do perfil
        │  faz fallback automático se o primário falhar
        ▼
 ILLMService
  ├── ClaudeService  (Anthropic)  → "claude-haiku"
  ├── OpenAIService  (OpenAI)     → "gpt-4o-mini"
  └── GeminiService  (Google)    → "gemini-flash"
        │
        ▼
 SSE Streaming → Frontend Vue.js
```

### Perfis e LLMs

| Perfil            | LLM primário    |
|-------------------|-----------------|
| Professor         | Claude Haiku    |
| Produtor Guaraná  | GPT-4o Mini     |
| Pescador/Agricultor | Gemini Flash  |
| Agente de Saúde   | Claude Haiku    |
| Servidor Público  | GPT-4o Mini     |
| Empreendedor      | Claude Haiku    |

---

## Stack

| Componente    | Tecnologia                  |
|---------------|-----------------------------|
| Runtime       | .NET 10                     |
| Framework     | ASP.NET Core Web API        |
| Logging       | Serilog (console)           |
| Validação     | FluentValidation            |
| Documentação  | Scalar (`/scalar/v1`)       |
| Testes        | xUnit + FluentAssertions + NSubstitute + Bogus |
| Deploy        | Railway (free tier)         |

---

## Fluxo de uma requisição

1. `POST /api/chat` com `{ "profileId": "professor", "message": "..." }`
2. FluentValidation verifica profileId (deve existir) e message (3–2000 chars)
3. ProfileService carrega o system prompt do perfil no `profiles.json`
4. LLMOrchestratorService identifica qual LLM usar e tenta em ordem com fallback
5. Resposta chega como SSE: cada chunk é `data: texto\n\n`
6. Evento final: `data: [DONE] {"success":true,"data":{"llmUsed":"...","profileId":"..."},...}\n\n`

---

## Tratamento de erros

| Cenário                  | HTTP | Resposta                              |
|--------------------------|------|---------------------------------------|
| profileId inválido       | 400  | `ApiResponse` com messageError        |
| Mensagem muito curta     | 400  | `ApiResponse` com messageError        |
| Todos os LLMs falharam   | 503  | `ApiResponse` com mensagem amigável   |
| Erro inesperado          | 500  | `ApiResponse` sem stack trace         |
| Resposta já iniciada     | —    | Log interno, sem corrupção do stream  |
