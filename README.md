# AjuriIA API

Backend do AjuriIA — plataforma de assistência por IA para a realidade amazônica.

## Setup Local

1. Instale .NET 10 SDK
2. Copie `appsettings.Development.json.example` para `appsettings.Development.json`
3. Preencha as chaves de API (ver `docs/setup/deploy-and-llms.md`)

```bash
dotnet run --project src/AjuriIA.API
```

Documentação: http://localhost:5000/scalar/v1

## Testes

```bash
dotnet test tests/AjuriIA.Tests --verbosity normal
```

## Deploy

Ver `docs/setup/deploy-and-llms.md` para instruções completas de Railway + Vercel.
