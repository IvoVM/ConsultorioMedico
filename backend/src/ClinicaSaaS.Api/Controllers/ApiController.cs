using ClinicaSaaS.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Api;

public abstract class ApiController : ControllerBase
{
    protected Task<IActionResult> Run<T>(Func<Task<T>> action) =>
        Guard(async () => Ok(await action()));

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
        BusinessRuleException rule => new(StatusCodes.Status400BadRequest, rule.Message),
        _ => null
    };
}

internal readonly record struct HttpError(int Status, string Message);
