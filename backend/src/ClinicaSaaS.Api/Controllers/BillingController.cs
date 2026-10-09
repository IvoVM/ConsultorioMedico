using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services;
using ClinicaSaaS.Application.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}")]
[Route("api")]
public class BillingController(BillingService billing) : ApiController
{
    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpGet("aranceles")]
    public Task<IActionResult> Fees(CancellationToken ct) => Run(() => billing.FeesAsync(ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("aranceles")]
    public Task<IActionResult> CreateFee(CreateFeeCommand command, CancellationToken ct) =>
        Run(() => billing.CreateFeeAsync(command, ct));

    [HttpGet("comprobantes")]
    public Task<IActionResult> Invoices(CancellationToken ct) => Run(() => billing.InvoicesAsync(ct));

    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pay(Guid id, PayInvoiceCommand command, CancellationToken ct) =>
        Run(() => billing.PayAsync(id, command, ct));
}
