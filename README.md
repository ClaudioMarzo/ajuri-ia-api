# AjuriIA API

> POC desenvolvida para feira de IA — assistente inteligente para comunidades amazônicas, com 6 perfis especializados, 3 LLMs com fallback automático e streaming em tempo real.

[![CI](https://github.com/ClaudioMarzo/ajuri-ia-api/actions/workflows/deploy.yml/badge.svg)](https://github.com/ClaudioMarzo/ajuri-ia-api/actions)
[![Testes](https://img.shields.io/badge/testes-41%20passing-brightgreen)](#testes)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Deploy](https://img.shields.io/badge/deploy-Fly.io%20%E2%80%94%20São%20Paulo-8B5CF6)](https://fly.io)

---

## Demo ao Vivo

Experimente agora — sem setup, direto da produção:

```bash
curl -N -X POST https://ajuri-ia-api.fly.dev/api/chat \
  -H "Content-Type: application/json" \
  -d '{"profileId":"professor","message":"Crie uma aula sobre guaraná para o 5º ano, 50 minutos"}'
```

> A flag `-N` desativa o buffer do curl — você vê o streaming acontecendo em tempo real, palavra por palavra.

Documentação interativa: **https://ajuri-ia-api.fly.dev/scalar/v1**

---

## Os 6 Perfis

Cada perfil tem um system prompt calibrado para sua realidade e um LLM ideal associado:

| Perfil | Descrição | LLM Ideal |
|--------|-----------|-----------|
| 📚 Professor / Educador | Planos de aula em segundos | Claude Haiku |
| 🌿 Produtor de Guaraná | Gestão e negócios para sua produção | GPT-4o Mini |
| 🐟 Pescador / Agricultor | Calendário, normas e acesso à assistência | Gemini Flash |
| 🏥 Agente de Saúde | Orientações e comunicados para a comunidade | Claude Haiku |
| 🏛️ Servidor Público | Ofícios, atas e documentos oficiais | GPT-4o Mini |
| 🚀 Empreendedor | Plano de negócio e pitch em minutos | Claude Haiku |

> **Nota POC:** nesta versão todos os perfis roteiam para Gemini Flash (custo zero). O campo `idealLlm` no `profiles.json` registra o LLM pretendido por perfil para quando as chaves das outras APIs estiverem configuradas.

---

## Como Funciona

```
POST /api/chat  { profileId, message }
       │
       ▼
 ChatController
       │  FluentValidation → profileId válido, message 3–2000 chars
       │  ProfileService   → carrega system prompt do profiles.json
       ▼
 LLMOrchestratorService
       │  tenta LLM primário do perfil
       │  fallback automático para os demais se o primário falhar
       ▼
 ILLMService
  ├── ClaudeService   → claude-haiku-4-5-20251001
  ├── OpenAIService   → gpt-4o-mini
  └── GeminiService   → gemini-2.0-flash
       │
       ▼
 SSE Streaming → chunks de texto em tempo real
 data: ## Plano de Aula\n
 data:  **Tema:** Guaraná\n
 data: [DONE] {"llmUsed":"gemini-flash","profileId":"professor",...}
```

**Decisões técnicas:**

- **SSE em vez de WebSocket** — resposta unidirecional; SSE é mais simples, funciona sem estado e é nativo no browser
- **Fallback entre LLMs** — o `LLMOrchestratorService` detecta falha no primeiro chunk e troca de provider transparentemente
- **Perfis em JSON externo** — novos perfis e system prompts sem recompilar
