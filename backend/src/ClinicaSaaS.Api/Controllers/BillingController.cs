using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class BillingController(FeeService fees, InvoiceService invoices, AccountService accounts) : ApiController
{
    private const string Desk = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}";

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpGet("aranceles")]
    public Task<IActionResult> Fees([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => fees.FeesResultAsync(query, ct));

    [Authorize(Roles = AppRoles.TenantAdmin)]
    [HttpPost("aranceles")]
    public Task<IActionResult> CreateFee(CreateFeeCommand command, CancellationToken ct) =>
        Run(() => fees.CreateFeeAsync(command, ct));

    [Authorize(Roles = Desk)]
    [HttpGet("comprobantes")]
    // The list keeps voided invoices apart: without voided it holds the ones in force, with it only the voided.
    public Task<IActionResult> Invoices([FromQuery] ListQuery query, [FromQuery] bool voided, CancellationToken ct) =>
        Run(() => invoices.InvoicesResultAsync(query, voided, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("comprobantes/mios")]
    public Task<IActionResult> MyInvoices(CancellationToken ct) =>
        Run(() => invoices.MineAsync(ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes")]
    public Task<IActionResult> Issue(IssueInvoiceCommand command, CancellationToken ct) =>
        Run(() => invoices.IssueAsync(command, ct));

    [Authorize(Roles = Desk)]
    [HttpPut("comprobantes/{id:guid}")]
    public Task<IActionResult> Update(Guid id, UpdateInvoiceCommand command, CancellationToken ct) =>
        Run(() => invoices.UpdateAsync(id, command, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/pagar")]
    public Task<IActionResult> Pay(Guid id, PayInvoiceCommand command, CancellationToken ct) =>
        Run(() => invoices.PayAsync(id, command, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/autorizar")]
    public Task<IActionResult> Authorize(Guid id, CancellationToken ct) =>
        Run(() => invoices.AuthorizeAsync(id, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/anular")]
    public Task<IActionResult> Void(Guid id, VoidInvoiceCommand command, CancellationToken ct) =>
        Run(() => invoices.VoidAsync(id, command, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/plan")]
    public Task<IActionResult> CreatePlan(Guid id, CreatePlanCommand command, CancellationToken ct) =>
        Run(() => invoices.CreatePlanAsync(id, command, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/plan/cancelar")]
    public Task<IActionResult> CancelPlan(Guid id, CancellationToken ct) =>
        Run(() => invoices.CancelPlanAsync(id, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("comprobantes/{id:guid}/aplicar-saldo")]
    public Task<IActionResult> ApplyCredit(Guid id, ApplyCreditCommand command, CancellationToken ct) =>
        Run(() => accounts.ApplyCreditAsync(id, command, ct));

    // Front desk prints any invoice; a patient only gets their own.
    [Authorize(Roles = $"{Desk},{AppRoles.Patient}")]
    [HttpGet("comprobantes/{id:guid}/pdf")]
    public Task<IActionResult> Pdf(Guid id, CancellationToken ct) =>
        RunFile(() => invoices.PdfAsync(id, ct));
}
