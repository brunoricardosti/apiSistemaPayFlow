# PayFlow - Camada de Pagamentos (Versão Final)

Esta versão inclui:
- Integração via HttpClient (configurável via `appsettings.json`)
- Resiliência com Polly (retry + backoff)
- Logs estruturados com Serilog
- Circuit-breaker suggestions (Polly) e observability notes
- Testes unitários com xUnit + Moq (project included)
- Dockerfile + docker-compose

## Como rodar (Docker)
```bash
docker-compose up --build
```
A API ficará acessível em `http://localhost:8080/payments`

## Como rodar local (dotnet)
```bash
cd src/PayFlow
dotnet restore
dotnet run
```

## Configuração (appsettings.json)
Ajuste `Providers:FastPay:BaseUrl` e `Providers:SecurePay:BaseUrl` para endpoints reais.
