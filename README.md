# AjuriIA API

Backend da plataforma AjuriIA — assistência por inteligência artificial construída para a realidade amazônica (Maués, AM).

O sistema recebe mensagens de 6 perfis (professor, produtor de guaraná, pescador, agente de saúde, servidor público, empreendedor) e responde via streaming usando Claude, GPT-4o Mini ou Gemini Flash com fallback automático.

---

## Obtendo as chaves de API

A API precisa de 3 chaves para funcionar. Todas têm plano gratuito suficiente para a demo.

---

### 1. Claude (Anthropic) — `CLAUDE_API_KEY`

Usado pelos perfis: Professor, Agente de Saúde, Empreendedor.

1. Acesse **https://console.anthropic.com**
2. Crie uma conta (pode usar e-mail ou Google)
3. No menu lateral, clique em **API Keys**
4. Clique em **Create Key**, dê um nome (ex: `ajuri-ia`)
5. Copie a chave — começa com `sk-ant-api03-...`

> **Custo estimado na demo:** ~R$ 0,50 para 50 respostas completas

---

### 2. OpenAI — `OPENAI_API_KEY`

Usado pelos perfis: Produtor de Guaraná, Servidor Público.

1. Acesse **https://platform.openai.com**
2. Crie uma conta
3. No menu superior direito, clique no seu perfil → **API Keys**
4. Clique em **Create new secret key**, dê um nome
5. Copie a chave — começa com `sk-proj-...` ou `sk-...`

> ⚠️ A OpenAI exige crédito pré-pago mínimo de **$5 USD** para a API funcionar. O gasto real da demo é centavos.
> Para adicionar crédito: **Settings → Billing → Add payment method**

---

### 3. Google Gemini — `GEMINI_API_KEY`

Usado pelo perfil: Pescador / Agricultor.

1. Acesse **https://aistudio.google.com**
2. Faça login com sua conta Google
3. No canto superior direito, clique em **Get API key**
4. Clique em **Create API key** → selecione ou crie um projeto Google Cloud
5. Copie a chave — começa com `AIza...`

> **Custo:** Totalmente gratuito no nível de uso da demo

---

## Configurando no Fly.io (produção)

Com as 3 chaves em mãos, acesse o painel do app em **https://fly.io/apps/ajuri-ia-api** e vá em:

**Secrets → Add a secret** — adicione cada uma:

| Nome da variável | Valor |
|-----------------|-------|
| `CLAUDE_API_KEY` | `sk-ant-api03-...` |
| `OPENAI_API_KEY` | `sk-...` |
| `GEMINI_API_KEY` | `AIza...` |

O Fly.io reinicia o app automaticamente após salvar cada secret.

Para verificar se funcionou:
```bash
curl https://ajuri-ia-api.fly.dev/api/health
# Esperado: {"status":"healthy"}
```

Para testar uma resposta real:
```bash
curl -N -X POST https://ajuri-ia-api.fly.dev/api/chat \
  -H "Content-Type: application/json" \
  -d '{"profileId":"professor","message":"Crie uma aula sobre guaraná para o 5º ano"}'
```

---

## Rodando localmente

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `make`

### Setup
```bash
# 1. Clonar o repositório
git clone https://github.com/ClaudioMarzo/ajuri-ia-api
cd ajuri-ia-api

# 2. Criar arquivo de configuração local
make setup

# 3. Preencher as chaves no arquivo criado
# Edite: src/AjuriIA.API/appsettings.Development.json
```

### Rodando
```bash
make run     # API em http://localhost:5000
make watch   # Com hot reload (reinicia ao salvar)
```

Documentação interativa: **http://localhost:5000/scalar/v1**

---

## Comandos disponíveis

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
| `GET` | `/api/profiles` | Lista os 6 perfis |
| `POST` | `/api/chat` | Chat com streaming SSE |
| `GET` | `/scalar/v1` | Documentação interativa |

Ver detalhes completos em [doc/api.md](doc/api.md).

---

## Arquitetura

```
POST /api/chat
    │
    ▼
ChatController → valida request → carrega system prompt
    │
    ▼
LLMOrchestratorService → tenta LLM primário → fallback automático
    │
    ├── ClaudeService   (claude-haiku-4-5-20251001)
    ├── OpenAIService   (gpt-4o-mini)
    └── GeminiService   (gemini-2.0-flash)
    │
    ▼
SSE Streaming → chunks de texto em tempo real
```

Ver arquitetura completa em [doc/visao-geral.md](doc/visao-geral.md).

---

## Stack

| | |
|---|---|
| Runtime | .NET 10 |
| Framework | ASP.NET Core Web API |
| Logging | Serilog |
| Validação | FluentValidation |
| Documentação | Scalar |
| Testes | xUnit + FluentAssertions + NSubstitute + Bogus |
| Deploy | Fly.io (São Paulo — `gru`) |
