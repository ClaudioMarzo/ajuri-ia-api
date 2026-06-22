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
