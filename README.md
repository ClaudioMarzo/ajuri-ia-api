# AjuriIA API

> POC desenvolvida para feira de IA — assistente inteligente para comunidades amazônicas, com 6 perfis especializados, 3 LLMs com fallback automático e streaming em tempo real.

[![CI](https://github.com/ClaudioMarzo/ajuri-ia-api/actions/workflows/deploy.yml/badge.svg)](https://github.com/ClaudioMarzo/ajuri-ia-api/actions)
[![Testes](https://img.shields.io/badge/testes-41%20passing-brightgreen)](#testes)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Deploy](https://img.shields.io/badge/deploy-Render-46E3B7)](https://render.com)

---

## Demo ao Vivo

Experimente agora — sem setup, direto da produção:

```bash
curl -N -X POST https://ajuri-ia.onrender.com/api/chat \
  -H "Content-Type: application/json" \
  -d '{"profileId":"professor","message":"Crie uma aula sobre guaraná para o 5º ano, 50 minutos"}'
```

> A flag `-N` desativa o buffer do curl — você vê o streaming acontecendo em tempo real, palavra por palavra.

Documentação interativa: **https://ajuri-ia.onrender.com/scalar/v1**

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

---

## Stack

| Componente | Tecnologia |
|------------|------------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core Web API |
| LLMs | Claude Haiku · GPT-4o Mini · Gemini Flash |
| Logging | Serilog |
| Validação | FluentValidation |
| Documentação | Scalar (`/scalar/v1`) |
| Testes | xUnit · FluentAssertions · NSubstitute · Bogus |
| Deploy | Fly.io (região São Paulo — `gru`) |
| CI/CD | GitHub Actions |

---

## Rodando Localmente

**Pré-requisitos:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) e `make`

```bash
# 1. Clonar
git clone https://github.com/ClaudioMarzo/ajuri-ia-api
cd ajuri-ia-api

# 2. Criar arquivo de configuração local
make setup

# 3. Preencher as chaves no arquivo criado
# Edite: src/AjuriIA.API/appsettings.Development.json
# {
#   "CLAUDE_API_KEY": "sk-ant-...",   → console.anthropic.com
#   "OPENAI_API_KEY": "sk-...",       → platform.openai.com/api-keys
#   "GEMINI_API_KEY": "AIza..."       → aistudio.google.com
# }

# 4. Subir
make run    # API em http://localhost:5000
make watch  # Com hot reload
```

> Para usar **apenas Gemini** (plano gratuito, sem cartão), preencha só `GEMINI_API_KEY` e ignore as demais — todos os perfis roteiam para Gemini nesta POC.

**Comandos disponíveis:**

```bash
make run                 # Sobe a API localmente
make watch               # Hot reload
make test                # Roda os 41 testes
make test-unit           # Só testes unitários
make test-integration    # Só testes de integração
make test-functional     # Só testes funcionais
make deploy              # Testa e faz deploy no Fly.io
make logs                # Logs da app em produção
make clean               # Limpa bin/ e obj/
```

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/health` | Status da API |
| `GET` | `/api/profiles` | Lista os 6 perfis com system prompts |
| `POST` | `/api/chat` | Chat com streaming SSE |
| `GET` | `/scalar/v1` | Documentação interativa |

Referência completa de request/response: [doc/api.md](doc/api.md)

---

## Testes

41 testes cobrindo três camadas:

| Camada | O que cobre |
|--------|-------------|
| **Unitários** | Cada service isolado (Claude, OpenAI, Gemini, Orchestrator, ProfileService, Validator, Middleware) |
| **Integração** | Endpoints via `WebApplicationFactory` com mocks de HTTP |
| **Funcionais** | Fluxo SSE completo end-to-end |

```bash
make test                # Todos os 41
make test-unit           # Rápido — sem HTTP
make test-integration    # Endpoints com mocks
make test-functional     # SSE streaming completo
```

---

## Contexto do Projeto

**Ajuri** vem do Nheengatu e significa *mutirão* — trabalhar junto. O nome reflete a proposta: tecnologia de IA colaborando com quem mais precisa de acesso a ela.

Maués (AM) é a capital mundial do guaraná. Produtores rurais, professores de escolas ribeirinhas, pescadores, agentes de saúde comunitária e servidores públicos municipais têm pouco ou nenhum acesso a ferramentas de IA calibradas para sua realidade.

Esta POC foi desenvolvida para uma **feira de IA** como demonstração de que é possível construir assistentes contextualizados — não IA genérica, mas IA que conhece o calendário de pesca do Amazonas, as normas do IBAMA, o cotidiano de uma escola ribeirinha e a realidade do produtor de guaraná de Maués.
