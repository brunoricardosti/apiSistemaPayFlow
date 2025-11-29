# PayFlow – Gateway de Pagamentos

Este documento descreve o sistema PayFlow, incluindo arquitetura, integrações, execução via Docker e variações avançadas (integração real, logs estruturados, testes unitários, observabilidade, etc.).

---

## 📘 **Visão Geral**
O *PayFlow* é um gateway de pagamentos simples com alternância automática entre provedores (FastPay e SecurePay). Ele recebe um payload padronizado e converte para o formato específico de cada provedor. Também calcula taxas, controla disponibilidade e retorna a resposta consolidada.

---

## 🏛️ **Arquitetura**
A arquitetura segue princípios de:
- **Strategy Pattern** para seleção de provedores.
- **Inversão de Dependência (DI)** com interfaces para provedores.
- **Providers isolados** responsáveis por montar payloads e interpretar respostas.
- **Service Central** (PaymentService) que:
  - escolhe o provedor adequado,
  - calcula taxas,
  - trata fallback,
  - normaliza a resposta.

### Estrutura Simplificada
```
src/
 └── PayFlow/
      ├── Controllers/
      ├── Providers/
      │     ├── IFastPayProvider.cs
      │     ├── ISecurePayProvider.cs
      │     └── Implementações
      ├── Services/
      │     └── PaymentService.cs
      ├── Models/
      ├── Program.cs
      ├── appsettings.json
      └── Dockerfile
```

---

## 🚀 **Fluxo da API**
### Endpoint
```
POST /payments
{
  "amount": 120.50,
  "currency": "BRL"
}
```
### Regras
- `< 100`: usar **FastPay**
- `>= 100`: usar **SecurePay**
- fallback automático se o provedor estiver indisponível
- resposta contém: `grossAmount`, `fee`, `netAmount`, `provider`, `status`, `externalId`

---

## 🧮 **Cálculo de Taxas**
- **FastPay:** `3.49%`
- **SecurePay:** `2.99% + 0.40`

Exemplo:
```
valor: 120.50
SecurePay fee:
  120.50 * 0.0299 = 3.60
  + 0.40 = 4.00
```

---

## 🐳 **Execução com Docker Compose**
```
docker-compose up --build
```
A API ficará disponível em:
```
http://localhost:8080/payments
```

---

# 📌 Variações e Extensões da Arquitetura
A seguir estão opções avançadas para deixar o PayFlow mais próximo de um gateway real.

---

# 1️⃣ Integração com Endpoints Reais (HTTPS)
Para uso real, cada provedor teria URL e credenciais.

### Exemplo de configuração (appsettings.json)
```json
{
  "Providers": {
    "FastPay": {
      "BaseUrl": "https://api.fastpay.com/pay",
      "ApiKey": "FASTPAY-KEY"
    },
    "SecurePay": {
      "BaseUrl": "https://securepay.com/charge",
      "Token": "SECUREPAY-TOKEN"
    }
  }
}
```

### Registro do HttpClient
```csharp
builder.Services.AddHttpClient<IFastPayProvider, FastPayProvider>(client =>
{
    client.BaseAddress = new Uri(config["Providers:FastPay:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Authorization", config["Providers:FastPay:ApiKey"]);
});
```

### Provider com HTTP real
```csharp
var response = await _client.PostAsJsonAsync("", payload);
response.EnsureSuccessStatusCode();
return await response.Content.ReadFromJsonAsync<FastPayResponse>();
```

---

# 2️⃣ Logs Estruturados (Serilog)
### Instalação
```
dotnet add package Serilog.AspNetCore
```

### Configuração
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
```

### Uso
```csharp
_logger.LogInformation("Processando pagamento {Amount} via {Provider}", amount, provider);
```

---

# 3️⃣ Testes Unitários
### Bibliotecas sugeridas
- xUnit
- FluentAssertions
- NSubstitute ou Moq

### Exemplo de Teste do PaymentService
```csharp
[Fact]
public async Task Deve_Usar_FastPay_Quando_Valor_Menor_Que_100()
{
    var fastMock = Substitute.For<IFastPayProvider>();
    var secureMock = Substitute.For<ISecurePayProvider>();

    fastMock.ProcessAsync(Arg.Any<PaymentRequest>())
        .Returns(new ProviderResult { Status = "approved" });

    var service = new PaymentService(fastMock, secureMock);

    var result = await service.ProcessAsync(50.0, "BRL");

    result.Provider.Should().Be("FastPay");
}
```

---

# 4️⃣ Testes de Integração
Criar WebApplicationFactory (para minimal API):
```csharp
public class ApiFactory : WebApplicationFactory<Program> { }
```

### Teste
```csharp
var client = factory.CreateClient();
var response = await client.PostAsJsonAsync("/payments", new { amount = 150, currency = "BRL" });
response.StatusCode.Should().Be(HttpStatusCode.OK);
```

---

# 5️⃣ Observabilidade (Opcional)
- OpenTelemetry
- Exportação para Grafana/Tempo/Prometheus

### Métricas úteis
- tempo de resposta dos provedores
- taxa de erro por provedor
- latency da API

---

# 6️⃣ Circuit Breaker para Provedores (Polly)
```csharp
builder.Services.AddHttpClient<IFastPayProvider, FastPayProvider>()
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));
```

---

# ✔️ Conclusão
O PayFlow é modular, expansível e pronto para produção com as variações incluídas:
- integração real via HTTP
- logs estruturados
- testes unitários e integração
- fallback e resiliência
- observabilidade

Se quiser, posso **gerar a versão completa do código atualizado**, incluindo logs, testes e clientes HTTP reais.

# PayFlow – Gateway de Pagamentos

Este documento descreve o sistema PayFlow, incluindo arquitetura, integrações, execução via Docker e variações avançadas (integração real, logs estruturados, testes unitários, observabilidade, etc.).

---

## 📘 **Visão Geral**
O *PayFlow* é um gateway de pagamentos simples com alternância automática entre provedores (FastPay e SecurePay). Ele recebe um payload padronizado e converte para o formato específico de cada provedor. Também calcula taxas, controla disponibilidade e retorna a resposta consolidada.

---

## 🏛️ **Arquitetura**
A arquitetura segue princípios de:
- **Strategy Pattern** para seleção de provedores.
- **Inversão de Dependência (DI)** com interfaces para provedores.
- **Providers isolados** responsáveis por montar payloads e interpretar respostas.
- **Service Central** (PaymentService) que:
  - escolhe o provedor adequado,
  - calcula taxas,
  - trata fallback,
  - normaliza a resposta.

### Estrutura Simplificada
```
src/
 └── PayFlow/
      ├── Controllers/
      ├── Providers/
      │     ├── IFastPayProvider.cs
      │     ├── ISecurePayProvider.cs
      │     └── Implementações
      ├── Services/
      │     └── PaymentService.cs
      ├── Models/
      ├── Program.cs
      ├── appsettings.json
      └── Dockerfile
```

---

## 🚀 **Fluxo da API**
### Endpoint
```
POST /payments
{
  "amount": 120.50,
  "currency": "BRL"
}
```
### Regras
- `< 100`: usar **FastPay**
- `>= 100`: usar **SecurePay**
- fallback automático se o provedor estiver indisponível
- resposta contém: `grossAmount`, `fee`, `netAmount`, `provider`, `status`, `externalId`

---

## 🧮 **Cálculo de Taxas**
- **FastPay:** `3.49%`
- **SecurePay:** `2.99% + 0.40`

Exemplo:
```
valor: 120.50
SecurePay fee:
  120.50 * 0.0299 = 3.60
  + 0.40 = 4.00
```

---

## 🐳 **Execução com Docker Compose**
```
docker-compose up --build
```
A API ficará disponível em:
```
http://localhost:8080/payments
```

---

# 📌 Variações e Extensões da Arquitetura
A seguir estão opções avançadas para deixar o PayFlow mais próximo de um gateway real.

---

# 1️⃣ Integração com Endpoints Reais (HTTPS)
Para uso real, cada provedor teria URL e credenciais.

### Exemplo de configuração (appsettings.json)
```json
{
  "Providers": {
    "FastPay": {
      "BaseUrl": "https://api.fastpay.com/pay",
      "ApiKey": "FASTPAY-KEY"
    },
    "SecurePay": {
      "BaseUrl": "https://securepay.com/charge",
      "Token": "SECUREPAY-TOKEN"
    }
  }
}
```

### Registro do HttpClient
```csharp
builder.Services.AddHttpClient<IFastPayProvider, FastPayProvider>(client =>
{
    client.BaseAddress = new Uri(config["Providers:FastPay:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Authorization", config["Providers:FastPay:ApiKey"]);
});
```

### Provider com HTTP real
```csharp
var response = await _client.PostAsJsonAsync("", payload);
response.EnsureSuccessStatusCode();
return await response.Content.ReadFromJsonAsync<FastPayResponse>();
```

---

# 2️⃣ Logs Estruturados (Serilog)
### Instalação
```
dotnet add package Serilog.AspNetCore
```

### Configuração
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
```

### Uso
```csharp
_logger.LogInformation("Processando pagamento {Amount} via {Provider}", amount, provider);
```

---

# 3️⃣ Testes Unitários
### Bibliotecas sugeridas
- xUnit
- FluentAssertions
- NSubstitute ou Moq

### Exemplo de Teste do PaymentService
```csharp
[Fact]
public async Task Deve_Usar_FastPay_Quando_Valor_Menor_Que_100()
{
    var fastMock = Substitute.For<IFastPayProvider>();
    var secureMock = Substitute.For<ISecurePayProvider>();

    fastMock.ProcessAsync(Arg.Any<PaymentRequest>())
        .Returns(new ProviderResult { Status = "approved" });

    var service = new PaymentService(fastMock, secureMock);

    var result = await service.ProcessAsync(50.0, "BRL");

    result.Provider.Should().Be("FastPay");
}
```

---

# 4️⃣ Testes de Integração
Criar WebApplicationFactory (para minimal API):
```csharp
public class ApiFactory : WebApplicationFactory<Program> { }
```

### Teste
```csharp
var client = factory.CreateClient();
var response = await client.PostAsJsonAsync("/payments", new { amount = 150, currency = "BRL" });
response.StatusCode.Should().Be(HttpStatusCode.OK);
```

---

# 5️⃣ Observabilidade (Opcional)
- OpenTelemetry
- Exportação para Grafana/Tempo/Prometheus

### Métricas úteis
- tempo de resposta dos provedores
- taxa de erro por provedor
- latency da API

---

# 6️⃣ Circuit Breaker para Provedores (Polly)
```csharp
builder.Services.AddHttpClient<IFastPayProvider, FastPayProvider>()
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));


# ✔️ Conclusão
O PayFlow é modular, expansível e pronto para produção com as variações incluídas:
- integração real via HTTP
- logs estruturados
- testes unitários e integração
- fallback e resiliência
- observabilidade
