# AjuriIA API

> POC desenvolvida para feira de IA — assistente inteligente para comunidades amazônicas, com perfis especializados, Gemini Flash e streaming em tempo real.

[![CI](https://github.com/ClaudioMarzo/ajuri-ia-api/actions/workflows/deploy.yml/badge.svg)](https://github.com/ClaudioMarzo/ajuri-ia-api/actions)
[![Testes](https://img.shields.io/badge/testes-44%20passing-brightgreen)](#testes)
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

## Os Perfis

Cada perfil tem um system prompt calibrado para sua realidade:

| Perfil | Descrição |
|--------|-----------|
| 📚 Professor / Educador | Planos de aula em segundos |
| 🏥 Agente de Saúde | Orientações e comunicados para a comunidade |

> Novos perfis e system prompts podem ser adicionados em [`profiles.json`](src/AjuriIA.API/Config/profiles.json) sem recompilar.

---

## Como Funciona

```
POST /api/chat  { profileId, message, model? }
       │
       ▼
 ChatController
       │  FluentValidation → profileId válido, message 3–2000 chars, model na allowlist
       │  ProfileService   → carrega system prompt do profiles.json
       │  model escolhido pelo cliente (ou default do servidor)
       ▼
 LLMOrchestratorService
       │  tenta o modelo escolhido; em 503/falha cai para os demais
       │  se todos falham → 503 (AllLLMsUnavailableException)
       ▼
 GeminiService (1 instância por modelo: gemini-2.5-flash, 2.0-flash, 3.5-flash)
       │  HttpClient resiliente (Polly): timeout + retry de rede/429
       ▼
 SSE Streaming → chunks de texto em tempo real
 data: ## Plano de Aula\n
 data:  **Tema:** Guaraná\n
 data: [DONE] {"llmUsed":"gemini-2.5-flash","profileId":"professor",...}
```

**Decisões técnicas:**

- **SSE em vez de WebSocket** — resposta unidirecional; SSE é mais simples, funciona sem estado e é nativo no browser
- **Cliente escolhe o modelo** — `model` no request (validado contra `GET /api/models`); o servidor faz fallback automático entre modelos em caso de sobrecarga (503) e informa em `llmUsed` qual respondeu
- **Resiliência HTTP** — chamada ao Gemini com timeout e retry resiliente (Polly via `Microsoft.Extensions.Http.Resilience`)
- **Perfis e modelos em config** — novos perfis (`profiles.json`) e modelos (`Gemini:Models`) sem recompilar

---

## Stack

| Componente | Tecnologia |
|------------|------------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core Web API |
| LLM | Gemini Flash |
| Resiliência | Polly (`Microsoft.Extensions.Http.Resilience`) |
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

# 3. Preencher a chave no arquivo criado
# Edite: src/AjuriIA.API/appsettings.Development.json
# {
#   "GEMINI_API_KEY": "AIza..."       → aistudio.google.com
# }

# 4. Subir
make run    # API em http://localhost:5000
make watch  # Com hot reload
```

> O Gemini tem plano gratuito (sem cartão) — basta gerar a chave em [aistudio.google.com](https://aistudio.google.com).

**Comandos disponíveis:**

```bash
make run                 # Sobe a API localmente
make watch               # Hot reload
make test                # Roda os 44 testes
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
| `GET` | `/api/profiles` | Lista os perfis com system prompts |
| `GET` | `/api/models` | Lista os modelos de IA disponíveis + default |
| `POST` | `/api/chat` | Chat com streaming SSE (aceita `model` opcional) |
| `GET` | `/scalar/v1` | Documentação interativa |

Referência completa de request/response: [doc/api.md](doc/api.md)

---

## Testes

44 testes cobrindo três camadas:

| Camada | O que cobre |
|--------|-------------|
| **Unitários** | Cada service isolado (Gemini, Orchestrator, ProfileService, Validator, Middleware) |
| **Integração** | Endpoints via `WebApplicationFactory` com mocks de HTTP |
| **Funcionais** | Fluxo SSE completo end-to-end |

```bash
make test                # Todos os 44
make test-unit           # Rápido — sem HTTP
make test-integration    # Endpoints com mocks
make test-functional     # SSE streaming completo
```

---

## Contexto do Projeto

**Ajuri** vem do Nheengatu e significa *mutirão* — trabalhar junto. O nome reflete a proposta: tecnologia de IA colaborando com quem mais precisa de acesso a ela.

Maués (AM) é a capital mundial do guaraná. Produtores rurais, professores de escolas ribeirinhas, pescadores, agentes de saúde comunitária e servidores públicos municipais têm pouco ou nenhum acesso a ferramentas de IA calibradas para sua realidade.

Esta POC foi desenvolvida para uma **feira de IA** como demonstração de que é possível construir assistentes contextualizados — não IA genérica, mas IA que conhece o calendário de pesca do Amazonas, as normas do IBAMA, o cotidiano de uma escola ribeirinha e a realidade do produtor de guaraná de Maués.
