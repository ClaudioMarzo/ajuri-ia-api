# AjuriIA API — Referência de Endpoints

Base URL local: `http://localhost:5000`
Base URL produção: `https://ajuri-ia.onrender.com`
Documentação interativa: `/scalar/v1`

---

## GET /api/health

Verifica se a API está no ar.

**Resposta 200:**
```json
{ "status": "healthy" }
```

---

## GET /api/profiles

Retorna os 2 perfis disponíveis com suas descrições.

**Resposta 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "professor",
      "nome": "Professor / Educador",
      "icone": "📚",
      "descricao": "Planos de aula em segundos",
      "llm": "gemini-flash",
      "systemPrompt": "..."
    }
  ],
  "traceId": "00-4bf92f...",
  "messageError": null
}
```

---

## GET /api/models

Lista os modelos de IA disponíveis para o cliente escolher e o modelo default.

**Resposta 200:**
```json
{
  "success": true,
  "data": {
    "default": "gemini-2.5-flash",
    "models": [
      { "id": "gemini-2.5-flash", "label": "Gemini 2.5 Flash" },
      { "id": "gemini-2.0-flash", "label": "Gemini 2.0 Flash" },
      { "id": "gemini-3.5-flash", "label": "Gemini 3.5 Flash" }
    ]
  },
  "traceId": "00-4bf92f...",
  "messageError": null
}
```

---

## POST /api/chat

Envia uma mensagem para um perfil e recebe a resposta em streaming (Server-Sent Events).

**Body:**
```json
{
  "profileId": "professor",
  "message": "Crie uma aula sobre guaraná para o 5º ano, 50 minutos",
  "model": "gemini-2.5-flash"
}
```

**Regras de validação:**
- `profileId`: obrigatório, deve ser um dos IDs cadastrados
- `message`: obrigatório, entre 3 e 2000 caracteres
- `model`: **opcional**; se informado, deve ser um dos IDs de `GET /api/models`. Se omitido, usa o default do servidor.

**IDs de perfil válidos:** `professor` · `agente-saude`

> **Fallback de modelo:** o servidor tenta o modelo escolhido primeiro; se ele estiver sobrecarregado (503), cai automaticamente para os demais modelos da lista e retorna a resposta do primeiro que funcionar. O campo `llmUsed` no evento `[DONE]` informa qual modelo de fato respondeu.

**Resposta 200 — stream SSE:**
```
Content-Type: text/event-stream

data: ## Plano de Aula\n

data:  **Tema:** Guaraná\n

data:  ...

data: [DONE] {"success":true,"data":{"llmUsed":"gemini-flash","profileId":"professor"},"traceId":"00-4bf9...","messageError":null}
```

**Resposta 400 — validação falhou:**
```json
{
  "success": false,
  "data": null,
  "traceId": "00-9a3c...",
  "messageError": "Perfil 'invalido' não encontrado."
}
```

**Resposta 503 — todos os LLMs indisponíveis:**
```json
{
  "success": false,
  "data": null,
  "traceId": "00-7d1e...",
  "messageError": "Serviço de IA temporariamente indisponível. Tente novamente em instantes."
}
```

---

## Testando com curl

```bash
# Health check
curl http://localhost:5000/api/health

# Listar perfis
curl http://localhost:5000/api/profiles | jq .

# Chat com streaming
curl -N -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"profileId":"professor","message":"Crie uma aula sobre guaraná para o 5º ano"}' 
```

> A flag `-N` no curl desativa o buffer e mostra o streaming em tempo real.
