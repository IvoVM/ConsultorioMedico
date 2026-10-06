using ClinicaSaaS.Application;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api;

public abstract class ApiController : ControllerBase
{
    protected Task<IActionResult> Ejecutar<T>(Func<Task<T>> accion) =>
        Proteger(async () => Ok(await accion()));

    protected Task<IActionResult> Ejecutar(Func<Task> accion) =>
        Proteger(async () =>
        {
            await accion();
            return NoContent();
        });

    private async Task<IActionResult> Proteger(Func<Task<IActionResult>> accion)
    {
        try
        {
            return await accion();
        }
        catch (Exception ex) when (MapaErrores.Traducir(ex) is { } error)
        {
            return StatusCode(error.Status, new { error = error.Mensaje });
        }
    }
}

internal static class MapaErrores
{
    public static ErrorHttp? Traducir(Exception ex) => ex switch
    {
        ValidationException validacion => new(StatusCodes.Status400BadRequest, string.Join(' ', validacion.Errors.Select(e => e.ErrorMessage))),
        NoEncontradoException noEncontrado => new(StatusCodes.Status404NotFound, noEncontrado.Message),
        ConflictoException conflicto => new(StatusCodes.Status409Conflict, conflicto.Message),
        ReglaNegocioException regla => new(StatusCodes.Status400BadRequest, regla.Message),
        _ => null
    };
}

internal readonly record struct ErrorHttp(int Status, string Mensaje);
