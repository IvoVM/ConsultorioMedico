using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}")]
[Route("api")]
public class BillingController(FeeService fees, InvoiceService invoices) : ApiController
{
    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpGet("aranceles")]
    public Task<IActionResult> Fees([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => fees.FeesResultAsync(query, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("aranceles")]
    public Task<IActionResult> CreateFee(CreateFeeCommand command, CancellationToken ct) =>
        Run(() => fees.CreateFeeAsync(command, ct));

    [HttpGet("comprobantes")]
    public Task<IActionResult> Invoices([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => invoices.InvoicesResultAsync(query, ct));

    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pay(Guid id, PayInvoiceCommand command, CancellationToken ct) =>
        Run(() => invoices.PayAsync(id, command, ct));
}
