using PayFlow.Models;
using PayFlow.Providers;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace PayFlow.Services;

public class PaymentService
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly ILogger<PaymentService> _logger;
    private static int _internalId = 1;

    public PaymentService(IEnumerable<IPaymentProvider> providers, ILogger<PaymentService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<PaymentResponse> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        IPaymentProvider? preferred = request.Amount < 100m ?
            _providers.FirstOrDefault(p => p.Name == "FastPay") :
            _providers.FirstOrDefault(p => p.Name == "SecurePay");

        var other = _providers.FirstOrDefault(p => p != preferred);

        var tried = new List<IPaymentProvider>();

        foreach (var provider in new[] { preferred, other }.Where(p => p != null))
        {
            tried.Add(provider!);
            try
            {
                _logger.LogInformation("Tentando processar pagamento via {Provider}", provider!.Name);
                var (success, externalId) = await provider!.ProcessPaymentAsync(request, ct);
                if (success)
                {
                    var fee = CalculateFee(provider!.Name, request.Amount);
                    var net = Decimal.Round(request.Amount - fee, 2, MidpointRounding.ToEven);
                    _logger.LogInformation("Pagamento aprovado por {Provider} (externalId={ExternalId})", provider.Name, externalId);
                    return new PaymentResponse
                    {
                        Id = Interlocked.Increment(ref _internalId),
                        ExternalId = externalId,
                        Status = "approved",
                        Provider = provider.Name,
                        GrossAmount = request.Amount,
                        Fee = fee,
                        NetAmount = net
                    };
                }
                else
                {
                    _logger.LogWarning("Provedor {Provider} retornou não aprovado", provider.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar com o provedor {Provider}", provider!.Name);
                // continue to next provider
            }
        }

        _logger.LogWarning("Todos os provedores falharam para o pagamento {Amount}", request.Amount);
        return new PaymentResponse
        {
            Id = Interlocked.Increment(ref _internalId),
            ExternalId = string.Empty,
            Status = "failed",
            Provider = string.Join(',', tried.Select(p => p.Name)),
            GrossAmount = request.Amount,
            Fee = 0m,
            NetAmount = 0m
        };
    }

    private decimal CalculateFee(string providerName, decimal amount)
    {
        if (providerName == "FastPay")
        {
            var fee = Math.Round(amount * 0.0349m, 2, MidpointRounding.ToEven);
            return fee;
        }
        else
        {
            var fee = Math.Round(amount * 0.0299m + 0.40m, 2, MidpointRounding.ToEven);
            return fee;
        }
    }
}
