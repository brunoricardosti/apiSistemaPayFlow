using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using PayFlow.Providers;
using PayFlow.Services;
using PayFlow.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace PayFlow.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task Deve_Usar_FastPay_Quando_Valor_Menor_Que_100()
    {
        var fast = new Mock<IPaymentProvider>();
        fast.SetupGet(f => f.Name).Returns("FastPay");
        fast.Setup(f => f.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "FP-12345"));

        var secure = new Mock<IPaymentProvider>();
        secure.SetupGet(s => s.Name).Returns("SecurePay");

        var logger = new Mock<ILogger<PaymentService>>();
        var service = new PaymentService(new[] { fast.Object, secure.Object }, logger.Object);

        var result = await service.ProcessAsync(new PaymentRequest { Amount = 50m, Currency = "BRL" });

        result.Provider.Should().Be("FastPay");
        result.Status.Should().Be("approved");
        result.Fee.Should().Be(Math.Round(50m * 0.0349m, 2));
    }

    [Fact]
    public async Task Deve_Tentar_Other_Se_Preferido_Falhar()
    {
        var fast = new Mock<IPaymentProvider>();
        fast.SetupGet(f => f.Name).Returns("FastPay");
        fast.Setup(f => f.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("down"));

        var secure = new Mock<IPaymentProvider>();
        secure.SetupGet(s => s.Name).Returns("SecurePay");
        secure.Setup(s => s.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "SP-99999"));

        var logger = new Mock<ILogger<PaymentService>>();
        var service = new PaymentService(new[] { fast.Object, secure.Object }, logger.Object);

        var result = await service.ProcessAsync(new PaymentRequest { Amount = 50m, Currency = "BRL" });

        result.Provider.Should().Be("SecurePay");
        result.Status.Should().Be("approved");
    }
}
