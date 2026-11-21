using System.Net;
using System.Text.Json;
using FluentValidation;
using FuelTrack.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Validation Error", ex.Errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteProblemDetails(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database update constraint violation");
            var (statusCode, title, message) = MapDbUpdateException(ex);
            await WriteProblemDetails(context, statusCode, title, message);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden access");
            await WriteProblemDetails(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError, "Internal Server Error", ex.Message);
        }
    }

    private static (int StatusCode, string Title, string Message) MapDbUpdateException(DbUpdateException ex)
    {
        var raw = $"{ex.Message} {ex.InnerException?.Message}".ToLowerInvariant();

        // Unique constraint violation (SQLite 19/UNIQUE, Postgres 23505/unique, SQL Server 2601/2627)
        if (raw.Contains("unique") || raw.Contains("duplicate") || raw.Contains("ux_") || raw.Contains("ix_") || raw.Contains("23505") || raw.Contains("2601") || raw.Contains("2627"))
        {
            if (raw.Contains("ux_fueltanks_activedailygenerator") || raw.Contains("generatorid"))
            {
                return (StatusCodes.Status409Conflict, "Conflict", "This generator already has a daily tank.");
            }
            if (raw.Contains("ix_fueltanks_substationid_fuellookupid"))
                return (StatusCodes.Status409Conflict, "Conflict", "A main tank for this fuel type already exists.");
            return (StatusCodes.Status409Conflict, "Conflict", "A record with the same unique value already exists.");
        }

        // Foreign key constraint violation (SQLite 19/FOREIGN KEY, Postgres 23503/foreign key, SQL Server 547)
        if (raw.Contains("foreign key") || raw.Contains("fk_") || raw.Contains("23503") || raw.Contains("547"))
        {
            return (StatusCodes.Status400BadRequest, "Bad Request", "A related record could not be found or a database foreign key constraint was violated.");
        }

        return (StatusCodes.Status400BadRequest, "Bad Request", "A database constraint violation occurred.");
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string title, object detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Instance = context.Request.Path,
            Detail = detail is string s ? s : null
        };

        if (detail is IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        {
            var errors = failures.Select(f => new
            {
                field = char.ToLowerInvariant(f.PropertyName[0]) + f.PropertyName[1..],
                message = f.ErrorMessage,
                attemptedValue = f.AttemptedValue
            });
            problem.Extensions["errors"] = errors;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var body = JsonSerializer.Serialize(problem, options);
        await context.Response.WriteAsync(body);
    }
}
