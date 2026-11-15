using Microsoft.EntityFrameworkCore;
using Npgsql;
using ClinicaSaaS.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api;

public abstract class ApiController : ControllerBase
{
    protected Task<IActionResult> Run<T>(Func<Task<T>> action) =>
        Guard(async () => Ok(await action()));

    protected Task<IActionResult> RunFile(Func<Task<FileDto>> action) =>
        Guard(async () =>
        {
            var file = await action();
            return File(file.Content, file.ContentType, file.Name);
        });

    protected Task<IActionResult> Run(Func<Task> action) =>
        Guard(async () =>
        {
            await action();
            return NoContent();
        });

    private async Task<IActionResult> Guard(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ErrorMap.Translate(ex) is { } error)
        {
            return StatusCode(error.Status, new { error = error.Message });
        }
    }
}

internal static class ErrorMap
{
    public static HttpError? Translate(Exception ex) => ex switch
    {
        UnauthorizedException unauthorized => new(StatusCodes.Status401Unauthorized, unauthorized.Message),
        ValidationException validation => new(StatusCodes.Status400BadRequest, string.Join(' ', validation.Errors.Select(e => e.ErrorMessage))),
        NotFoundException notFound => new(StatusCodes.Status404NotFound, notFound.Message),
        ConflictException conflict => new(StatusCodes.Status409Conflict, conflict.Message),
        AccountChoiceException choice => new(StatusCodes.Status409Conflict, choice.Message),
        BusinessRuleException rule => new(StatusCodes.Status400BadRequest, rule.Message),
        // The database refuses two active appointments at the same time in one room or on one piece of equipment
        // (EX_Appointments_RoomNoOverlap, EX_Appointments_EquipmentNoOverlap) or of one professional (EX_Appointments_NoOverlap).
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: "EX_Appointments_RoomNoOverlap" } } =>
            new(StatusCodes.Status409Conflict, "Ese consultorio se acaba de ocupar a esa hora. Elegí otro horario."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: "EX_Appointments_EquipmentNoOverlap" } } =>
            new(StatusCodes.Status409Conflict, "Ese equipo se acaba de ocupar a esa hora. Elegí otro horario."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation } } =>
            new(StatusCodes.Status409Conflict, "Ese horario se acaba de ocupar. Elegí otro."),
        _ => null
    };
}

internal readonly record struct HttpError(int Status, string Message);
