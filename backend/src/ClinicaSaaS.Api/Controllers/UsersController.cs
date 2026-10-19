using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/usuarios")]
public class UsersController(UsersService users) : ApiController
{
    [HttpGet]
    public Task<IActionResult> List([FromQuery] ListQuery query, CancellationToken ct) =>
        Run(() => users.ListAsync(query, ct));

    [HttpPost("{id:guid}/reiniciar-clave")]
    public Task<IActionResult> ResetPassword(Guid id, CancellationToken ct) =>
        Run(() => users.ResetPasswordAsync(id, ct));
}
