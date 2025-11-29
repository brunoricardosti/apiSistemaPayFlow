using Microsoft.AspNetCore.Mvc;
using PayFlow.Models;
using PayFlow.Services;
namespace PayFlow.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PaymentRequest request, CancellationToken ct)
    {
        if (request == null || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Currency))
            return BadRequest(new { error = "Invalid payload" });

        _logger.LogInformation("Recebido pagamento {Amount} {Currency}", request.Amount, request.Currency);
        var resp = await _paymentService.ProcessAsync(request, ct);
        return Ok(resp);
    }
}
