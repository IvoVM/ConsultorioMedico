using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

// A patient's running account: front desk reads and moves anyone's, a patient only reads their own.
[ApiController]
[Authorize]
[Route("api/cuentas")]
public class AccountsController(AccountService accounts) : ApiController
{
    private const string Desk = $"{AppRoles.TenantAdmin},{AppRoles.Secretary}";

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("mia")]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(() => accounts.MineAsync(ct));

    [Authorize(Roles = Desk)]
    [HttpGet("{clientId:guid}")]
    public Task<IActionResult> Get(Guid clientId, CancellationToken ct) => Run(() => accounts.GetAsync(clientId, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("{clientId:guid}/cobros")]
    public Task<IActionResult> Pay(Guid clientId, AccountPaymentCommand command, CancellationToken ct) => Run(() => accounts.PayAsync(clientId, command, ct));

    [Authorize(Roles = Desk)]
    [HttpPost("{clientId:guid}/devoluciones")]
    public Task<IActionResult> Refund(Guid clientId, RefundCommand command, CancellationToken ct) => Run(() => accounts.RefundAsync(clientId, command, ct));
}
