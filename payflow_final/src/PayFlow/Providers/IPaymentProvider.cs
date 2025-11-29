using PayFlow.Models;
namespace PayFlow.Providers;
public interface IPaymentProvider
{
    string Name { get; }
    Task<(bool success, string externalId)> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
}

public interface IFastPayProvider : IPaymentProvider { }
public interface ISecurePayProvider : IPaymentProvider { }
