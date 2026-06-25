API     := src/AjuriIA.API
TESTS   := tests/AjuriIA.Tests
PORT    := 5000

.PHONY: run watch build test test-unit test-integration test-functional clean setup help

help:
	@echo ""
	@echo "  Desenvolvimento:"
	@echo "  make run                 Sobe a API em http://localhost:$(PORT)"
	@echo "  make watch               Sobe com hot reload (reinicia ao salvar)"
	@echo "  make build               Compila o projeto"
	@echo "  make test                Roda todos os testes"
	@echo "  make test-unit           Roda apenas testes unitários"
	@echo "  make test-integration    Roda apenas testes de integração"
	@echo "  make test-functional     Roda apenas testes funcionais"
	@echo "  make setup               Cria appsettings.Development.json com as chaves"
	@echo "  make clean               Remove bin/ e obj/"
	@echo ""
	@echo "  Deploy (Render):"
	@echo "  Deploy automático via push para main (GitHub → Render)"
	@echo ""

run: build
	ASPNETCORE_URLS=http://localhost:$(PORT) dotnet run --project $(API) --no-build

watch:
	ASPNETCORE_URLS=http://localhost:$(PORT) dotnet watch --project $(API)

build:
	dotnet build --configuration Debug --verbosity quiet

test:
	dotnet test $(TESTS) --verbosity normal

test-unit:
	dotnet test $(TESTS) --filter "FullyQualifiedName~.Unit." --verbosity normal

test-integration:
	dotnet test $(TESTS) --filter "FullyQualifiedName~.Integration." --verbosity normal

test-functional:
	dotnet test $(TESTS) --filter "FullyQualifiedName~.Functional." --verbosity normal

clean:
	dotnet clean --verbosity quiet
	find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null; true
	find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null; true

setup:
	@if [ -f $(API)/appsettings.Development.json ]; then \
		echo "appsettings.Development.json já existe. Edite diretamente em $(API)/appsettings.Development.json"; \
	else \
		echo '{\n  "GEMINI_API_KEY": "AIza-COLOQUE-AQUI",\n  "AllowedOrigin": "http://localhost:5173"\n}' > $(API)/appsettings.Development.json; \
		echo "Criado $(API)/appsettings.Development.json — preencha as chaves antes de rodar."; \
	fi

