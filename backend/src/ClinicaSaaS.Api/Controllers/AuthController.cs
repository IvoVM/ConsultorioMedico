using ClinicaSaaS.Services;
using ClinicaSaaS.Services.Utilities;
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

    [HttpPost("ingreso-paciente")]
    public Task<IActionResult> LoginPatient(LoginPatientCommand command, CancellationToken ct) =>
        Issue(() => auth.LoginPatientAsync(command, ct));

    [HttpPost("ingreso-empleado")]
    public Task<IActionResult> LoginStaff(LoginStaffCommand command, CancellationToken ct) =>
        Issue(() => auth.LoginStaffAsync(command, ct));

    [Authorize]
    [HttpPost("/api/cuenta/clave")]
    public Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken ct) =>
        Issue(() => auth.ChangePasswordAsync(command, ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpGet("/api/cuenta/perfil")]
    public Task<IActionResult> Profile(CancellationToken ct) =>
        Run(() => auth.ProfileAsync(ct));

    [Authorize(Roles = AppRoles.Patient)]
    [HttpPut("/api/cuenta/perfil")]
    public Task<IActionResult> UpdateProfile(UpdatePatientProfileCommand command, CancellationToken ct) =>
        Run(() => auth.UpdateProfileAsync(command, ct));

    [HttpPost("registro")]
    public Task<IActionResult> Register(RegisterPatientCommand command, CancellationToken ct) =>
        Issue(() => auth.RegisterPatientAsync(command, ct));

    [HttpPost("renovar")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        try
        {
            var issued = await auth.RefreshAsync(PresentedRefresh(), ct);
            Remember(issued);
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
        await auth.LogoutAsync(PresentedRefresh(), ct);
        RefreshCookie.Clear(Response);
        return NoContent();
    }

    private string? PresentedRefresh() =>
        Request.Cookies[RefreshCookie.Name] is { Length: > 0 } cookie
            ? cookie
            : Request.Headers[RefreshHeader.Name].FirstOrDefault();

    private void Remember(IssuedSession issued)
    {
        RefreshCookie.Set(Response, issued.RefreshToken, issued.RefreshExpires);
        Response.Headers[RefreshHeader.Name] = issued.RefreshToken;
    }

    private async Task<IActionResult> Issue(Func<Task<IssuedSession>> action)
    {
        try
        {
            var issued = await action();
            Remember(issued);
            return Ok(issued.Access);
        }
        catch (Exception ex) when (ErrorMap.Translate(ex) is { } error)
        {
            return StatusCode(error.Status, new { error = error.Message });
        }
    }
}
