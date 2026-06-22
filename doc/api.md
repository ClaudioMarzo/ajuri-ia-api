# AjuriIA API — Referência de Endpoints

Base URL local: `http://localhost:5000`
Base URL produção: `https://ajuri-ia-api.up.railway.app`
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

Retorna os 6 perfis disponíveis com suas descrições.

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
      "llm": "claude-haiku",
      "systemPrompt": "..."
    }
  ],
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
  "message": "Crie uma aula sobre guaraná para o 5º ano, 50 minutos"
}
```

**Regras de validação:**
- `profileId`: obrigatório, deve ser um dos 6 IDs cadastrados
- `message`: obrigatório, entre 3 e 2000 caracteres

**IDs válidos:** `professor` · `produtor` · `pescador` · `agente-saude` · `servidor` · `empreendedor`

**Resposta 200 — stream SSE:**
```
Content-Type: text/event-stream

data: ## Plano de Aula\n

data:  **Tema:** Guaraná\n

data:  ...

data: [DONE] {"success":true,"data":{"llmUsed":"claude-haiku","profileId":"professor"},"traceId":"00-4bf9...","messageError":null}
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
