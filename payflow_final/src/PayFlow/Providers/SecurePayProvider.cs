using System.Text.Json;
using System.Text;
using PayFlow.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace PayFlow.Providers;

public class SecurePayProvider : ISecurePayProvider
{
    private readonly HttpClient _client;
    private readonly ILogger<SecurePayProvider> _logger;

    public string Name => "SecurePay";

    public SecurePayProvider(HttpClient client, ILogger<SecurePayProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(bool success, string externalId)> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var payload = new {
            amount_cents = (int)(request.Amount * 100),
            currency_code = request.Currency,
            client_reference = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        if (_client.BaseAddress != null)
        {
            _logger.LogInformation("Enviando para SecurePay (real) payload: {@Payload}", payload);
            var resp = await _client.PostAsJsonAsync(string.Empty, payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("SecurePay retornou status {Status}", resp.StatusCode);
                throw new Exception("SecurePay unavailable or returned error");
            }
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var result = body.GetProperty("result").GetString();
            var id = body.GetProperty("transaction_id").GetString();
            return (result == "success", id ?? string.Empty);
        }
        else
        {
            _logger.LogInformation("SecurePay em modo simulado, payload: {@Payload}", payload);
            await Task.Delay(120, ct);
            var externalId = $"SP-{new Random().Next(10000,99999)}";
            return (true, externalId);
        }
    }
}
