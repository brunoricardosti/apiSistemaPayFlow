using System.Text.Json;
using System.Text;
using PayFlow.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace PayFlow.Providers;

public class FastPayProvider : IFastPayProvider
{
    private readonly HttpClient _client;
    private readonly ILogger<FastPayProvider> _logger;

    public string Name => "FastPay";

    public FastPayProvider(HttpClient client, ILogger<FastPayProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(bool success, string externalId)> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var payload = new {
            transaction_amount = request.Amount,
            currency = request.Currency,
            payer = new { email = "cliente@teste.com" },
            installments = 1,
            description = "Compra via FastPay"
        };

        // If client has BaseAddress configured, call real API; otherwise simulate
        if (_client.BaseAddress != null)
        {
            _logger.LogInformation("Enviando para FastPay (real) payload: {@Payload}", payload);
            var resp = await _client.PostAsJsonAsync(string.Empty, payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("FastPay retornou status {Status}", resp.StatusCode);
                throw new Exception("FastPay unavailable or returned error");
            }
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var status = body.GetProperty("status").GetString();
            var id = body.GetProperty("id").GetString();
            return (status == "approved", id ?? string.Empty);
        }
        else
        {
            _logger.LogInformation("FastPay em modo simulado, payload: {@Payload}", payload);
            await Task.Delay(120, ct);
            var externalId = $"FP-{DateTime.UtcNow.Ticks % 1000000}";
            return (true, externalId);
        }
    }
}
