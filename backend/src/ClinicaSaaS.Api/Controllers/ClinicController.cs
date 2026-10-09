using ClinicaSaaS.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/clinica")]
public class ClinicController(ICurrentTenant clinic) : ControllerBase
{
    [HttpGet]
    public ClinicDto Get() => new(clinic.Slug, clinic.Name);
}
