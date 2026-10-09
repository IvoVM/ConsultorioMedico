using ClinicaSaaS.Application;
using ClinicaSaaS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/acceso")]
public class AuthController(AuthService auth) : ApiController
{
    [HttpPost("ingreso")]
    public Task<IActionResult> Login(LoginCommand command, CancellationToken ct) =>
        Issue(() => auth.LoginTenantAsync(command, ct));

    [HttpPost("registro")]
    public Task<IActionResult> Register(RegisterPatientCommand command, CancellationToken ct) =>
        Issue(() => auth.RegisterPatientAsync(command, ct));

    [HttpPost("renovar")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        try
        {
            var issued = await auth.RefreshAsync(Request.Cookies[RefreshCookie.Name], ct);
            RefreshCookie.Set(Response, issued.RefreshToken, issued.RefreshExpires);
            return Ok(issued.Access);
        }
        catch (UnauthorizedException ex)
        {
            RefreshCookie.Clear(Response);
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("salir")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(Request.Cookies[RefreshCookie.Name], ct);
        RefreshCookie.Clear(Response);
        return NoContent();
    }

    private async Task<IActionResult> Issue(Func<Task<IssuedSession>> action)
    {
        try
        {
            var issued = await action();
            RefreshCookie.Set(Response, issued.RefreshToken, issued.RefreshExpires);
            return Ok(issued.Access);
        }
        catch (Exception ex) when (ErrorMap.Translate(ex) is { } error)
        {
            return StatusCode(error.Status, new { error = error.Message });
        }
    }
}
